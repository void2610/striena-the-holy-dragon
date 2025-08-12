using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MainCreditData", menuName = "ScriptableObjects/MainCreditData", order = 1)]
public class MainCreditData : ScriptableObject
{
    [Serializable]
    public class CreditData
    {
        public string name;
        public string role;
        public Sprite icon;
        public string link;
    }
    
    [SerializeField] private List<CreditData> credits;
    public List<CreditData> Credits => credits;
}
