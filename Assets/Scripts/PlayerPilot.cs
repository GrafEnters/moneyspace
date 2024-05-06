using System;
using UnityEngine;

public class PlayerPilot : MonoBehaviour {
    [SerializeField]
    private IShip _ship;

    [SerializeField]
    private bool _isMouseTarget = false;

    [SerializeField]
    private float _minDistanceToRotate = 50;

    [SerializeField]
    private float _minRotationMuliplier = 0.1f;

    private void Start() {
        _ship.name = "PlayerShip";
        _ship.OnDestroyed += OnShipDestroyed;
    }

    private void OnShipDestroyed() {
        RespawnManager.Instance.MinusPoint(Team.Blue);
        Respawn();
    }

    private void Respawn() {
        Transform spawnPoint = SpawnPoints.GetRandomSpawnPoint(Team.Blue);
        transform.SetParent(spawnPoint);
        _ship.transform.position = spawnPoint.position;
        _ship.gameObject.SetActive(true);
        _ship.Respawn();
    }

    private void Update() {
        GameUI.Instance._playerHpView.SetData(_ship.GetHpPercent(), 0);
        GameUI.Instance._arView.SetData(_ship.GetSpeedPercent(), 0);
        if (Input.GetKey(KeyCode.W)) {
            _ship.Accelerate();
        }

        if (Input.GetKey(KeyCode.S)) {
            _ship.Slowdown();
        }

        if (Input.GetMouseButtonDown(0)) {
            FirePrime();
        }

        if (Input.GetMouseButtonDown(1)) {
            FireSecond();
        }

        Vector2 shift = Input.mousePosition - new Vector3(Screen.width, Screen.height) / 2;
        if (shift.magnitude < _minDistanceToRotate) {
            shift *= _minRotationMuliplier;
        } else {
            shift -= shift.normalized * _minDistanceToRotate;
        }

        Vector3 rotVector = new Vector3(-shift.y, shift.x, 0);

        _ship.RotateForward(rotVector + TrySideRotate());
    }

    private void FirePrime() {
        Vector3 screenPos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        Vector3 point = ray.GetPoint(10000);
        _ship.FirePrime(point);
    }

    private void FireSecond() {
        Vector3 screenPos = _isMouseTarget ? Input.mousePosition : new Vector3(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        Vector3 point = ray.GetPoint(10000);
        _ship.FireSecond(point);
    }

    private Vector3 TrySideRotate() {
        Vector3 sideRot = Vector3.zero;
        if (Input.GetKey(KeyCode.E)) {
            sideRot += Vector3.back;
        }

        if (Input.GetKey(KeyCode.Q)) {
            sideRot += Vector3.forward;
        }

        return sideRot;
    }
}