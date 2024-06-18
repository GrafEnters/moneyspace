using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : IShip {
    [SerializeField]
    private int _shiedRepairSpeed = 10;

    [SerializeField]
    private float _overheatFromShoot = 0.1f, _overheatFromSecond = 0.025f;

    [SerializeField]
    private float decreaseOverheatSpeed = 0.1f;

    [SerializeField]
    private float _shipMaxSpeed = 3;

    [SerializeField]
    private float _accelerationSpeed = 4f;

    [SerializeField]
    private float _horRotation = 1, _vertRotation = 1;

    [SerializeField]
    private float _verticalMaxRotationSpeed = 10, _horizontalMaxRotationSpeed = 10;

    [SerializeField]
    private float _modelRotation = 30f;

    [SerializeField]
    private float _modelMovement = 30f;

    [SerializeField]
    private Transform _model;

    [SerializeField]
    private ShipThrust _shipThrust;

    [SerializeField]
    private Explosion _explosion;

    [SerializeField]
    private List<LaserCanon> _primeCanons = new List<LaserCanon>();

    [SerializeField]
    private List<LaserCanon> _secondCanons = new List<LaserCanon>();

    [SerializeField]
    private Transform _cameraFollowTarget;

    [SerializeField]
    private Shield _shield3dView;

    [SerializeField]
    private GameObject _warpOnShift;

    [SerializeField]
    private AudioSource _shipSounds;

    [SerializeField]
    private AudioClip _shipShot;

    [SerializeField]
    private float _decelerationSpeed = 1f;

    private float _shipSpeed = 0;

    public float ShipSpeed => _shipSpeed;

    private float _rotationSpeed = 0;

    private bool _recoil = false;

    private float _overheat;
    private int _hp;
    private float _shield;

    private Dictionary<PlayerData, int> _damageDealers = new Dictionary<PlayerData, int>();

    private float _timeCounter;
    private float _MaxTimeForCounter = 15;
    private bool _isOverheated = false;

    private void Start() {
        //Cursor.visible = false;
    }

    private void FixedUpdate() {
        FlyForward();
        DecreaseOverheat();
        RepairShield();

        _shipThrust.SetThrustLight(_shipSpeed / _shipMaxSpeed);

        if (transform.position.x > Math.Abs(5000) || transform.position.y > Math.Abs(5000) || transform.position.z > Math.Abs(5000)) {
            Debug.Log($"Вы вылетели за пределы боевой зоны у вас осталось {_MaxTimeForCounter - _timeCounter} секунд что бы вернуться");
            _timeCounter += Time.fixedDeltaTime;

            if (_timeCounter > _MaxTimeForCounter) {
                TakeDamage(100000, _owner);
            }
        } else {
            _timeCounter = 0;
        }
    }

    private void RepairShield() {
        if (_shield >= MaxShied) {
            return;
        }

        _shield += _shiedRepairSpeed * Time.fixedDeltaTime;
        if (_shield > MaxShied) {
            _shield = MaxShied;
        }
    }

    private void DecreaseOverheat() {
        if (_overheat <= 0) {
            return;
        }

        _overheat -= decreaseOverheatSpeed * Time.fixedDeltaTime;
        if (_overheat <= 0) {
            _overheat = 0;
            _isOverheated = false;
        }
    }

    public override void RotateBy(Vector3 rotVector) {
        MoveModel(rotVector);

        rotVector.x = Mathf.Clamp(rotVector.x, -_verticalMaxRotationSpeed, _verticalMaxRotationSpeed);
        rotVector.y = Mathf.Clamp(rotVector.y, -_verticalMaxRotationSpeed, _verticalMaxRotationSpeed);
        rotVector.z = Mathf.Clamp(rotVector.z, -_horizontalMaxRotationSpeed, _horizontalMaxRotationSpeed);

        Vector3 rotDistance = new Vector3(rotVector.x * _vertRotation, rotVector.y * _vertRotation, rotVector.z * _horRotation) *
                              Time.deltaTime;
        transform.rotation *= Quaternion.Euler(rotDistance);
    }

    public override float GetSpeedPercent() {
        return _shipSpeed / _shipMaxSpeed;
    }

    public override float GetOverheatPercent() {
        return _overheat;
    }

    public override float GetHpPercent() {
        return _hp / (_shipConfig.MaxHp + 0f);
    }

    public override float GetShieldPercent() {
        return _shield / (MaxShied + 0f);
    }

    private void MoveModel(Vector3 rotVector) {
        Vector3 modelRotVector = new Vector3(rotVector.x * _vertRotation, 0, -rotVector.y * _vertRotation) * _modelRotation;
        _model.localRotation = Quaternion.Euler(modelRotVector);
        Vector3 modelShift = new Vector3(rotVector.y, -rotVector.x, 0);
        modelShift.z = 0;
        _model.localPosition = modelShift * _modelMovement;
    }

    private void FlyForward() {
        transform.position += transform.forward * (_shipSpeed * Time.fixedDeltaTime);
    }

    public override void Accelerate() {
        _shipSpeed += _accelerationSpeed;
        _shipSpeed = Mathf.Clamp(_shipSpeed, 0, _shipMaxSpeed);
    }

    public override void Slowdown() {
        _shipSpeed -= _decelerationSpeed;
        _shipSpeed = Mathf.Clamp(_shipSpeed, 0, _shipMaxSpeed);
    }

    public override void SlowdownKeyDown() {
        _shipSpeed -= _accelerationSpeed;
        _shipSpeed = Mathf.Clamp(_shipSpeed, 0, _shipMaxSpeed);
    }

    public override void FirePrime(Vector3 target) {
        if (_recoil || _isOverheated || !gameObject.activeSelf) {
            return;
        }

        _overheat += _overheatFromShoot;
        if (_overheat >= 1) {
            _overheat = 1;
            _isOverheated = true;
        }

        _shipSounds.PlayOneShot(_shipShot);

        StartCoroutine(RecoilCoroutine(0.3f));
        foreach (var VARIABLE in _primeCanons) {
            VARIABLE.Shoot(target, _owner);
        }
    }

    private IEnumerator RecoilCoroutine(float delay) {
        _recoil = true;
        yield return new WaitForSeconds(delay);
        _recoil = false;
    }

    public override void FireSecond(Vector3 target) {
        if (_recoil || _isOverheated || !gameObject.activeSelf) {
            return;
        }

        _overheat += _overheatFromSecond;
        if (_overheat >= 1) {
            _overheat = 1;
            _isOverheated = true;
        }

        StartCoroutine(RecoilCoroutine(0.03f));
        foreach (var VARIABLE in _secondCanons) {
            VARIABLE.Shoot(target, _owner);
        }
    }

    public override void TakeDamage(int amount, AbstractPilot fromPilot) {
        PlayerData from = fromPilot.PlayerData;
        float damageThroughShield = amount - _shield;
        _shield -= amount;
        if (_shield >= 0) {
            _shield3dView.ShowShield();
            return;
        } else {
            _shield = 0;
        }

        if (damageThroughShield <= 0) {
            return;
        }

        _hp -= Mathf.RoundToInt(damageThroughShield);

        if (_damageDealers.ContainsKey(from)) {
            _damageDealers[from] += amount;
        } else {
            _damageDealers.Add(from, amount);
        }

        if (_hp < 0 && gameObject.activeSelf) {
            _hp = 0;
            _owner.PlayerData.Deaths++;
            from.Kills++;
            foreach (var kvp in _damageDealers) {
                if (kvp.Key != from) {
                    kvp.Key.Assists++;
                }
            }

            OnDestroyed?.Invoke(_owner, fromPilot);
            Explode();
        }
    }

    public override void Respawn() {
        _shipSpeed = _shipMaxSpeed / 2;
        _hp = _shipConfig.MaxHp;
        _overheat = 0;
        _isOverheated = false;
        _recoil = false;
    }

    public override Transform GetCameraFollowTarget() {
        return _cameraFollowTarget;
    }

    private void Explode() {
        Instantiate(_explosion, transform.position, transform.rotation);
        gameObject.SetActive(false);
    }
}