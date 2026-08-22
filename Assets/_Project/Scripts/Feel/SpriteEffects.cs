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
    /// <b>Every write reads the existing block back first.</b> A
    /// <see cref="SpriteRenderer"/> supplies its own sprite texture through a property block, and
    /// <see cref="Renderer.SetPropertyBlock(MaterialPropertyBlock)"/> replaces the block entirely
    /// rather than merging into it — so setting a value without reading first silently discards
    /// the texture binding and leaves an untextured quad. It shows up as a sprite flickering
    /// between its real shape and a plain rectangle, worst on pooled objects that are enabled and
    /// damaged constantly.
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

            // Reused rather than allocated per write, since the flash tween writes every frame.
            _properties = new MaterialPropertyBlock();

            ResetAll();
        }

        /// <summary>Whitens the sprite. 0 is untouched, 1 is fully the flash colour.</summary>
        public void SetFlash(Color colour, float amount)
        {
            _renderer.GetPropertyBlock(_properties);

            _properties.SetColor(FlashColourId, colour);
            _properties.SetFloat(FlashAmountId, Mathf.Clamp01(amount));

            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>Blends the sprite towards a colour. Used for attack wind-up tells.</summary>
        public void SetTint(Color colour, float amount)
        {
            _renderer.GetPropertyBlock(_properties);

            _properties.SetColor(TintColourId, colour);
            _properties.SetFloat(TintAmountId, Mathf.Clamp01(amount));

            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>
        /// Multiplies the sprite by a colour, and stays applied. Used to darken the boss as the
        /// phases escalate.
        /// </summary>
        public void SetPhaseTint(Color colour)
        {
            _renderer.GetPropertyBlock(_properties);

            _properties.SetColor(PhaseTintId, colour);

            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>Burns the sprite away. 0 is whole, 1 is gone.</summary>
        public void SetDissolve(float amount)
        {
            _renderer.GetPropertyBlock(_properties);

            _properties.SetFloat(DissolveAmountId, Mathf.Clamp01(amount));

            _renderer.SetPropertyBlock(_properties);
        }

        /// <summary>
        /// Returns the sprite to its untouched appearance.
        /// </summary>
        /// <remarks>
        /// Pooled objects need this on reuse: an instance returned to the pool mid-flash or
        /// part-dissolved would otherwise be handed out again still wearing it.
        /// </remarks>
        public void ResetAll()
        {
            _renderer.GetPropertyBlock(_properties);

            _properties.SetColor(FlashColourId, Color.white);
            _properties.SetFloat(FlashAmountId, 0f);
            _properties.SetColor(TintColourId, Color.white);
            _properties.SetFloat(TintAmountId, 0f);
            _properties.SetColor(PhaseTintId, Color.white);
            _properties.SetFloat(DissolveAmountId, 0f);

            _renderer.SetPropertyBlock(_properties);
        }
    }
}
