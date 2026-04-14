// LaserBeamVisual.cs
// Attach this to the LaserBeamVisual prefab root.
// It holds references to the three LineRenderers and the impact FX.
// The BeamAttackBehaviour controls everything through this single component.

using UnityEngine;

public class LaserBeamVisual : MonoBehaviour
{
    [Header("Line Renderers (assign in prefab)")]
    public LineRenderer coreLine;
    public LineRenderer glowLine;
    public LineRenderer haloLine;

    [Header("Impact FX (assign in prefab)")]
    public ParticleSystem impactFX;

    [Header("Default Widths (set in prefab, code multiplies these)")]
    public float coreWidth = 0.08f;
    public float glowWidth = 0.3f;
    public float haloWidth = 0.6f;

    /// <summary>
    /// Applies per-boss colors to all three lines.
    /// </summary>
    public void SetColors(Color core, Color glow, Color halo)
    {
        if (coreLine != null)
        {
            coreLine.startColor = core;
            coreLine.endColor = core;
        }
        if (glowLine != null)
        {
            glowLine.startColor = glow;
            glowLine.endColor = glow;
        }
        if (haloLine != null)
        {
            haloLine.startColor = halo;
            haloLine.endColor = halo;
        }
    }

    /// <summary>
    /// Sets the width of all three lines, scaled by a multiplier.
    /// The ratio between core/glow/halo is preserved from the prefab defaults.
    /// </summary>
    public void SetWidth(float multiplier)
    {
        if (coreLine != null)
        {
            coreLine.startWidth = coreWidth * multiplier;
            coreLine.endWidth = coreWidth * multiplier;
        }
        if (glowLine != null)
        {
            glowLine.startWidth = glowWidth * multiplier;
            glowLine.endWidth = glowWidth * multiplier;
        }
        if (haloLine != null)
        {
            haloLine.startWidth = haloWidth * multiplier;
            haloLine.endWidth = haloWidth * multiplier;
        }
    }

    /// <summary>
    /// Sets the width as a fraction of the final width (for warm-up animation).
    /// t=0 means zero width, t=1 means full width * multiplier.
    /// </summary>
    public void SetWidthNormalized(float t, float multiplier)
    {
        float m = t * multiplier;
        if (coreLine != null)
        {
            coreLine.startWidth = coreWidth * m;
            coreLine.endWidth = coreWidth * m;
        }
        if (glowLine != null)
        {
            glowLine.startWidth = glowWidth * m;
            glowLine.endWidth = glowWidth * m;
        }
        if (haloLine != null)
        {
            haloLine.startWidth = haloWidth * m;
            haloLine.endWidth = haloWidth * m;
        }
    }

    /// <summary>
    /// Updates the start and end positions for all three lines.
    /// </summary>
    public void SetPositions(Vector3 origin, Vector3 end)
    {
        if (coreLine != null)
        {
            coreLine.SetPosition(0, origin);
            coreLine.SetPosition(1, end);
        }
        if (glowLine != null)
        {
            glowLine.SetPosition(0, origin);
            glowLine.SetPosition(1, end);
        }
        if (haloLine != null)
        {
            haloLine.SetPosition(0, origin);
            haloLine.SetPosition(1, end);
        }
    }

    /// <summary>
    /// Shows or hides all three lines.
    /// </summary>
    public void SetLinesEnabled(bool enabled)
    {
        if (coreLine != null) coreLine.enabled = enabled;
        if (glowLine != null) glowLine.enabled = enabled;
        if (haloLine != null) haloLine.enabled = enabled;
    }

    /// <summary>
    /// Starts the impact particle effect at the given position,
    /// oriented to face away from the beam direction.
    /// </summary>
    public void ShowImpact(Vector3 position, Vector3 beamDirection)
    {
        if (impactFX == null) return;

        impactFX.transform.position = position;

        // Point the impact FX away from the beam (sparks fly outward from hit point)
        if (beamDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(-beamDirection.y, -beamDirection.x) * Mathf.Rad2Deg;
            impactFX.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (!impactFX.isPlaying)
            impactFX.Play(true);
    }

    /// <summary>
    /// Stops the impact effect.
    /// </summary>
    public void HideImpact()
    {
        if (impactFX == null) return;
        impactFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    /// <summary>
    /// Full shutdown — hide everything.
    /// </summary>
    public void Deactivate()
    {
        SetLinesEnabled(false);
        SetWidthNormalized(0f, 1f);
        HideImpact();
    }
}