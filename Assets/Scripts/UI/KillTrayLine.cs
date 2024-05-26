using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KillTrayLine : MonoBehaviour {
    [SerializeField]
    private TextMeshProUGUI _killerNameText, _victimNameText;

    [SerializeField]
    private Image _killerShipIcon;

    public void SetData(AbstractPilot killer, AbstractPilot victim) {
        _killerNameText.text = killer.PlayerData.Nickname;
        _killerNameText.color = killer.PlayerData.Team == Team.Blue ? Color.blue : Color.red;
        _killerShipIcon.sprite = ShipsFactory.Ships.First(s => s.ShipType == killer.ShipType).Icon;
        _victimNameText.text = victim.PlayerData.Nickname;
        _victimNameText.color = victim.PlayerData.Team == Team.Blue ? Color.blue : Color.red;
    }
}