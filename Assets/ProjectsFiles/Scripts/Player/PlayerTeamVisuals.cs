using Unity.Netcode;
using UnityEngine;

// Uses the existing visual mesh and team textures; does not replace hierarchy references.
[RequireComponent(typeof(PlayerState))]
public sealed class PlayerTeamVisuals : NetworkBehaviour
{
    [SerializeField] private Renderer[] armorRenderers;
    [SerializeField] private Material blueArmor;
    [SerializeField] private Material redArmor;
    private PlayerState state;
    private MaterialPropertyBlock properties;
    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    private void Awake()
    {
        state = GetComponent<PlayerState>();
        properties = new MaterialPropertyBlock();
        if (armorRenderers == null || armorRenderers.Length == 0 || blueArmor == null || redArmor == null)
            Debug.LogError("PlayerTeamVisuals: required Inspector references armorRenderers, blueArmor and redArmor are missing.", this);
    }

    public override void OnNetworkSpawn()
    {
        state.teamIndex.OnValueChanged += OnTeamChanged;
        ApplyTeam(state.teamIndex.Value); // Includes late join / already assigned teams.
    }

    private void OnTeamChanged(int previous, int current) => ApplyTeam(current);

    private void ApplyTeam(int team)
    {
        Material armor = team == 1 ? redArmor : blueArmor;
        if (armor == null || armorRenderers == null) return;
        foreach (Renderer renderer in armorRenderers)
        {
            if (renderer == null)
            {
                Debug.LogError("PlayerTeamVisuals: armorRenderers contains a missing Renderer reference.", this);
                continue;
            }
            renderer.GetPropertyBlock(properties);
            // Property blocks survive CurvedWorldView's standard/curved material swaps.
            properties.SetTexture(BaseMap, armor.GetTexture(BaseMap));
            properties.SetTexture(MainTex, armor.GetTexture(BaseMap));
            renderer.SetPropertyBlock(properties);
        }
    }

    public override void OnNetworkDespawn()
    {
        state.teamIndex.OnValueChanged -= OnTeamChanged;
        ApplyTeam(-1);
    }
}
