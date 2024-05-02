using System.Collections;
using UnityEngine;

public class BotPilot : MonoBehaviour {
    [SerializeField]
    private IShip _ship;

    private Transform _target;

    [SerializeField]
    private Team _team;

    private BotState _state;
    private float _distToTarget;
    private float _shipSpeed;

    private float _maxShootDistance = 100;
    private float _shootDistanceRandomness = 25;

    private Coroutine _shootCoroutine;
    //attack random parameters
    private float _shootDistance;
    private float _desirableSpeed;
    private float _attackTime;
    

    public void SetTeam(Team type) {
        _team = type;
    }

    private void Start() {
        FindTarget();
    }

    private void FindTarget() {
        _target = GameObject.FindWithTag(_team == Team.Blue ? "Red" : "Blue").transform;
        RndAttackParameters();
        _state = BotState.Hunt;
        _shootCoroutine = StartCoroutine(ShootCoroutine());
    }

    private void Update() {
        _distToTarget = CalculateDist();
        _shipSpeed = _ship.GetSpeedPercent();

        if (_state == BotState.Hunt) {
            Hunt();
        } else if (_state == BotState.Shoot) {
            Shoot();
        } else if (_state == BotState.Evade) {
            Evade();
        }
    }

    private void Hunt() {
        if (_distToTarget < _shootDistance) {
            _state = BotState.Shoot;
            _shootCoroutine = StartCoroutine(ShootCoroutine());
            return;
        }

        if (_shipSpeed < 1) {
            _ship.Accelerate();
        }

        Vector3 dir = _target.position - _ship.transform.position;
        RotateShip(dir);
    }

    private void Shoot() {
        if (_distToTarget < 20) {
            _state = BotState.Evade;
            StopCoroutine(_shootCoroutine);
            return;
        }

        _ship.FirePrime(_target.position);

        if (_shipSpeed < _desirableSpeed) {
            _ship.Accelerate();
        } else {
            _ship.Slowdown();
        }

        Vector3 dir = _target.position - _ship.transform.position;
        RotateShip(dir);
    }

    private IEnumerator ShootCoroutine() {
        yield return new WaitForSeconds(_attackTime);
        _state = BotState.Evade;
    }

    private void Evade() {
        if (_distToTarget > _shootDistance) {
            _state = BotState.Hunt;
            RndAttackParameters();
            return;
        }

        if (_shipSpeed < 1) {
            _ship.Accelerate();
        }

        Vector3 dir = _ship.transform.position - _target.position;
        RotateShip(dir);
    }

    private void RotateShip(Vector3 dir) {
        Vector3 rotVector = Quaternion.FromToRotation(_ship.transform.forward, dir).eulerAngles;
        _ship.RotateForward(rotVector);
    }

    private void RndAttackParameters() {
        _state = BotState.Hunt;
        _shootDistance = _maxShootDistance + Random.Range(-1, 1f) * _shootDistanceRandomness;
        _desirableSpeed = Random.Range(0.2f, 0.8f);
        _attackTime = Random.Range(3, 10f);
    }

    private float CalculateDist() => Vector3.Magnitude(_target.position - _ship.transform.position);

    enum BotState {
        Hunt,
        Shoot,
        Evade
    }
}