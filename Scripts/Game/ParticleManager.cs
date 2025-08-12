using System.Collections.Generic;
using UnityEngine;
using Void2610.UnityTemplate;

public class ParticleManager : SingletonMonoBehaviour<ParticleManager>
{
    [System.Serializable]
    public class ParticleData
    {
        public string name;
        public ParticleSystem particlePrefab;
        public Vector2 position;
        public float scale = 1f;
    }
    
    [SerializeField] private List<ParticleData> particleDataList = new();
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true; // エディタでパーティクル位置を表示
    
    public void PlayParticle(string particleName)
    {
        var data = particleDataList.Find(p => p.name == particleName);
        if (data == null) return;
        
        var p = Instantiate(data.particlePrefab, this.transform);
        p.transform.localScale = Vector3.one * data.scale;
        p.transform.localPosition = new Vector3(data.position.x, data.position.y, 0f);
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        
        foreach (var pd in particleDataList)
        {
            var worldPos = transform.TransformPoint(pd.position);
            Gizmos.DrawWireSphere(worldPos, 0.5f);
            UnityEditor.Handles.Label(worldPos, pd.name);
        }
    }
#endif 
}