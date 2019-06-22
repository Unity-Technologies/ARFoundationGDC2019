using UnityEngine;

public class LightEstimationEmission : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer m_RobotMeshRenderer = null;
    [SerializeField] SkinnedMeshRenderer m_TeraMeshRendererBody = null;
    [SerializeField] SkinnedMeshRenderer m_TeraMeshRendererHead = null;

    static string k_EmissionReference = "_Emission";

    LightEstimation m_LightEstimation;

    void Awake()
    {
        m_LightEstimation = FindObjectOfType<LightEstimation>();
    }
 
    void Update()
    {
        if (m_LightEstimation.brightness.HasValue)
        {
            // robot body
            m_RobotMeshRenderer.sharedMaterials[0].SetFloat(k_EmissionReference, m_LightEstimation.brightness.Value);

            // robot head
            m_RobotMeshRenderer.sharedMaterials[2].SetFloat(k_EmissionReference, m_LightEstimation.brightness.Value);

            // teraformer
            m_TeraMeshRendererBody.sharedMaterials[0].SetFloat(k_EmissionReference, m_LightEstimation.brightness.Value);
            m_TeraMeshRendererHead.sharedMaterials[0].SetFloat(k_EmissionReference, m_LightEstimation.brightness.Value);
        }
    }
}
