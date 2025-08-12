using UnityEngine;

public class MainCreditView : MonoBehaviour
{
    [SerializeField] private GameObject creditItemPrefab;

    public void Initialize(MainCreditData mainCreditData)
    {
        foreach (var credit in mainCreditData.Credits)
        {
            var creditItem = Instantiate(creditItemPrefab, this.transform);
            SetUpCreditItem(creditItem, credit);
        }
    }
    
    private void SetUpCreditItem(GameObject creditItem, MainCreditData.CreditData creditData)
    {
        var nameText = creditItem.transform.Find("NameText").GetComponent<TMPro.TextMeshProUGUI>();
        var roleText = creditItem.transform.Find("RoleText").GetComponent<TMPro.TextMeshProUGUI>();
        var iconImage = creditItem.transform.Find("IconMask").Find("IconImage").GetComponent<UnityEngine.UI.Image>();
        var linkButton = iconImage.GetComponent<UnityEngine.UI.Button>();

        nameText.text = creditData.name;
        roleText.text = creditData.role;
        iconImage.sprite = creditData.icon;
        
        linkButton.onClick.AddListener(() => Application.OpenURL(creditData.link));
    }
}
