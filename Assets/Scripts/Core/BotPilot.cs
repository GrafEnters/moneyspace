using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class BotPilot : AbstractPilot {
    private Transform _target;

    private BotState _state;
    private float _distToTarget;
    private float _shipSpeed;
    private Coroutine _attackCoroutine;

    //attack random parameters
    private float _startShootDistance;
    private float _attackTime;

    private EnemyMarker _marker;
    private Coroutine _respawnCoroutine;
    private Vector3 _cachedRandomVector;
    private float _cachedRandomValue;

    public override void Init() {
        GetShip();
        CreateMarker();
        RespawnShip();
    }

    public override void Activate() {
        base.Activate();
        FindTarget();
    }

    public override void DeActivate() {
        base.DeActivate();
        if (_respawnCoroutine != null) {
            StopCoroutine(_respawnCoroutine);
        }
    }

    protected override ShipType GetShipType() {
        return (ShipType)Random.Range(0, ShipsFactory.Ships.Count);
    }

    protected override void GetShip() {
        base.GetShip();
        var shipConfig = ShipsFactory.Ships.First(s => s.ShipType == _ship.ShipType);
        _ship.InitFromDefaultConfig(shipConfig);
        _ship.OnDestroyed += StartRespawning;
    }

    private void CreateMarker() {
        if (_marker != null) {
            return;
        }

        if (_playerData.Team == Team.Red) {
            _marker = GameUI.Instance.ArMarkersManager.CreateRedMarker();
        } else {
            _marker = GameUI.Instance.ArMarkersManager.CreateBlueMarker();
        }

        _marker.SetTarget(_ship.transform);
        _ship.VisibleChecker.OnVisibleAction += () => { ChangeMarkerVisibility(true); };
        _ship.VisibleChecker.OnInvisibleAction += () => { ChangeMarkerVisibility(false); };
    }

    private void ChangeMarkerVisibility(bool isActive) {
        if (_marker)
            _marker.gameObject.SetActive(isActive);
    }

    private void FindTarget() {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(_playerData.Team == Team.Blue ? "Red" : "Blue");
        _target = targets[Random.Range(0, targets.Length)].transform;
        if (_target == null) {
            Debug.Log("no target found!");
        }

        RndAttackParameters();
        _state = BotState.Hunt;
        _attackCoroutine = StartCoroutine(AttackCoroutine());
    }

    private void Update() {
        if (_state == BotState.Respawn) {
            return;
        }

        if (_target != null) {
            _distToTarget = CalculateDist();
        } else {
            _distToTarget = 100000;
            FindTarget();
        }

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
        if (_distToTarget < _startShootDistance) {
            _state = BotState.Shoot;
            StopCoroutine(_attackCoroutine);
            _attackCoroutine = StartCoroutine(AttackCoroutine());
            _cachedRandomVector = Random.insideUnitSphere;
          
            return;
        }

        Vector3 dir = (_target.position + _cachedRandomVector * ShipsFactory.ShipStatsGeneralConfig.BotRandomChasePointDelta - _ship.transform.position).normalized;

        //todo add smooth lerp rotation
        if (Vector3.Angle(_ship.transform.forward, dir) > 60 + (1 - _cachedRandomValue) * 30 &&
            _shipSpeed > ShipsFactory.ShipStatsGeneralConfig.BotHuntDesirableSpeed + _cachedRandomValue / 5) {
            _ship.Slowdown();
        } else if (_shipSpeed < 1) {
            _ship.Accelerate();
        }

        RotateShip(dir);
    }

    private void Shoot() {
        if (_distToTarget < ShipsFactory.ShipStatsGeneralConfig.StartEvadeDistance) {
            _state = BotState.Evade;
            StopCoroutine(_attackCoroutine);
            _cachedRandomVector = Random.insideUnitSphere;
            return;
        }

        if (Vector3.Angle(_ship.transform.forward, _target.position - _ship.transform.position) <
            ShipsFactory.ShipStatsGeneralConfig.BotShootAngle) {
            Vector3 shootDelta = Random.Range(0, 1f) < ShipsFactory.ShipStatsGeneralConfig.BotRandomShootChance
                ? Random.insideUnitSphere * ShipsFactory.ShipStatsGeneralConfig.BotRandomShootDelta
                : Vector3.zero;
            _ship.FirePrime(_target.position + shootDelta);
        }

        if (_shipSpeed < ShipsFactory.ShipStatsGeneralConfig.BotShootDesirableSpeed) {
            _ship.Accelerate();
        } else {
            _ship.Slowdown();
        }

        Vector3 dir = _target.position - _ship.transform.position;
        RotateShip(dir);
    }

    private IEnumerator AttackCoroutine() {
        yield return new WaitForSeconds(_attackTime);
        _state = BotState.Evade;
        FindTarget();
    }


    
    private void Evade() {
        if (_distToTarget > _startShootDistance) {
            _state = BotState.Hunt;
            RndAttackParameters();
            return;
        }

        if (_shipSpeed < ShipsFactory.ShipStatsGeneralConfig.BotEvadeDesirableSpeed) {
            _ship.Accelerate();
        }

        Vector3 dir = _ship.transform.position + _cachedRandomVector * ShipsFactory.ShipStatsGeneralConfig.BotRandomEvadePointDelta - _target.position;
        RotateShip(dir);
    }

    private void RotateShip(Vector3 to) {
        to = to.normalized;
        float distFromCenter = Vector3.Distance(_ship.transform.position, Vector3.zero);
        if (distFromCenter > MainConfigTable.Instance.MainGameConfig.FightRadius) {
            float rotPercent = (distFromCenter - MainConfigTable.Instance.MainGameConfig.FightRadius) / 100;
            Vector3 dirToCenter = -_ship.transform.position.normalized;
            to = Vector3.Lerp(to, dirToCenter, rotPercent);
        }

        //_ship.transform.rotation = Quaternion.LookRotation(to);
        
        Quaternion finRotation = Quaternion.LookRotation(to);
        Quaternion rotation = Quaternion.RotateTowards(_ship.transform.rotation, finRotation, ShipsFactory.ShipStatsGeneralConfig.TechicalParams._vertRotation);// FromToRotation(_ship.transform.forward, to);
        Vector3 rotVector = rotation.eulerAngles;

        // Vector3 from = _ship.transform.forward;  
        // Vector3 rotVector = new Vector3(Vector3.SignedAngle(from, to, -_ship.transform.up), Vector3.SignedAngle(from, to, _ship.transform.right));
        _ship.RotateTo(finRotation);
    }

    private void RndAttackParameters() {
        _state = BotState.Hunt;
        var cnfg = ShipsFactory.ShipStatsGeneralConfig;
        _startShootDistance = Random.Range(cnfg.BotMinStartShootDistance, cnfg.BotMaxStartShootDistance);
        _attackTime = Random.Range(cnfg.BotMinAttackTime, cnfg.BotMaxAttackTime);
        _cachedRandomValue = Random.Range(0, 1f);
    }

    private float CalculateDist() => Vector3.Magnitude(_target.position - _ship.transform.position - (_state == BotState.Evade ? _cachedRandomVector * ShipsFactory.ShipStatsGeneralConfig.BotRandomEvadePointDelta : Vector3.zero));

    private void StartRespawning(AbstractPilot _, AbstractPilot __) {
        GameManager.Instance.RespawnManager.MinusPoint(_playerData.Team);
        _respawnCoroutine = StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine() {
        _state = BotState.Respawn;
        yield return new WaitForSeconds(MainConfigTable.Instance.MainGameConfig.BotRespawnTime);
        if (!_isActive) {
            yield break;
        }
        RespawnShip();
        FindTarget();
    }

    enum BotState {
        Hunt,
        Shoot,
        Evade,
        Respawn
    }


    private void OnDrawGizmosSelected() {
        if (_ship == null) {
            return;
        }
        Debug.Log(_state);
        if (_target != null) {
            Gizmos.color = PlayerData.Team == Team.Blue ? Color.blue :  Color.red;
            Gizmos.DrawLine(_ship.transform.position, _target.position);
        }
     
    }
}