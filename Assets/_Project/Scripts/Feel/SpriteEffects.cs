using UnityEngine;

namespace BossLevel.Feel
{
    /// <summary>
    /// Drives the shader properties on one sprite: hit flash, telegraph tint, phase tint and
    /// death dissolve.
    /// </summary>
    /// <remarks>
    /// Everything that wants to change how a sprite looks goes through here rather than writing
    /// <see cref="SpriteRenderer.color"/>. That is not tidiness — it is the fix for a real bug.
    /// The damage flash and the attack telegraph both used to write that one colour, so whichever
    /// ran second erased the other, and a hit landing during a wind-up simply did not register
    /// visually. As separate shader properties they compose instead of competing.
    /// <para>
    /// Values are pushed through a <see cref="MaterialPropertyBlock"/> so every sprite can hold
    /// its own without each one instantiating a copy of the material.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public class SpriteEffects : MonoBehaviour
    {
        private static readonly int FlashColourId = Shader.PropertyToID("_FlashColour");
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int TintColourId = Shader.PropertyToID("_TintColour");
        private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");
        private static readonly int PhaseTintId = Shader.PropertyToID("_PhaseTint");
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        private SpriteRenderer _renderer;
        private MaterialPropertyBlock _properties;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _properties = new MaterialPropertyBlock();

            ResetAll();
        }

        /// <summary>Whitens the sprite. 0 is untouched, 1 is fully the flash colour.</summary>
        public void SetFlash(Color colour, float amount)
        {
            _properties.SetColor(FlashColourId, colour);
            _properties.SetFloat(FlashAmountId, Mathf.Clamp01(amount));
            Apply();
        }

        /// <summary>Blends the sprite towards a colour. Used for attack wind-up tells.</summary>
        public void SetTint(Color colour, float amount)
        {
            _properties.SetColor(TintColourId, colour);
            _properties.SetFloat(TintAmountId, Mathf.Clamp01(amount));
            Apply();
        }

        /// <summary>
        /// Multiplies the sprite by a colour, and stays applied. Used to darken the boss as the
        /// phases escalate.
        /// </summary>
        public void SetPhaseTint(Color colour)
        {
            _properties.SetColor(PhaseTintId, colour);
            Apply();
        }

        /// <summary>Burns the sprite away. 0 is whole, 1 is gone.</summary>
        public void SetDissolve(float amount)
        {
            _properties.SetFloat(DissolveAmountId, Mathf.Clamp01(amount));
            Apply();
        }

        /// <summary>Returns the sprite to its untouched appearance.</summary>
        public void ResetAll()
        {
            _properties.SetColor(FlashColourId, Color.white);
            _properties.SetFloat(FlashAmountId, 0f);
            _properties.SetColor(TintColourId, Color.white);
            _properties.SetFloat(TintAmountId, 0f);
            _properties.SetColor(PhaseTintId, Color.white);
            _properties.SetFloat(DissolveAmountId, 0f);

            Apply();
        }

        private void Apply()
        {
            _renderer.SetPropertyBlock(_properties);
        }
    }
}
