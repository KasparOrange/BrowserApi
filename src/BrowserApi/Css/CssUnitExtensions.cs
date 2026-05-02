namespace BrowserApi.Css;

/// <summary>
/// C# 14 extension properties on numeric types that produce CSS value structs
/// — the spec §1 ergonomic shape: <c>16.Px</c> (not <c>16.Px()</c>).
/// </summary>
/// <remarks>
/// <para>
/// Spec §1 reads "Values read like CSS — <c>16.Px</c> not <c>Length.Px(16)</c>
/// or <c>16.Px()</c>." This class is the realization of that. Each extension
/// block adds parameterless properties to <see cref="int"/> or <see cref="double"/>;
/// the property body delegates to the corresponding <see cref="Length"/> /
/// <see cref="Duration"/> / <see cref="Angle"/> / <see cref="Percentage"/> /
/// <see cref="Flex"/> static factory.
/// </para>
/// <para>
/// Container-query units (<c>cqw</c>, <c>cqh</c>, <c>cqi</c>, <c>cqb</c>,
/// <c>cqmin</c>, <c>cqmax</c>) live here too — same shape, same naming, no
/// reason to split into a separate file.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Length
/// Length margin    = 16.Px;
/// Length fontSize  = 1.5.Rem;
/// Length width     = 100.Vw;
/// Length minHeight = 50.Cqh;
///
/// // Duration / Angle / Percentage / Flex
/// Duration fast    = 200.Ms;
/// Duration slow    = 0.5.S;
/// Angle rotation   = 45.Deg;
/// Percentage half  = 50.Percent;
/// Flex column      = 1.Fr;
/// </code>
/// </example>
/// <seealso cref="Length"/>
/// <seealso cref="Duration"/>
/// <seealso cref="Angle"/>
/// <seealso cref="Percentage"/>
/// <seealso cref="Flex"/>
public static class CssUnitExtensions {
    extension(int value) {
        // ─── Length — absolute, font-relative, viewport-relative ────────────────
        /// <summary>Pixels — absolute length.</summary>
        public Length Px  => Length.Px(value);
        /// <summary>Viewport height percentage (1vh = 1% of viewport height).</summary>
        public Length Vh  => Length.Vh(value);
        /// <summary>Viewport width percentage (1vw = 1% of viewport width).</summary>
        public Length Vw  => Length.Vw(value);

        // ─── Length — container-query units (spec §32) ──────────────────────────
        /// <summary>1% of the containing query container's width.</summary>
        public Length Cqw => Length.Cqw(value);
        /// <summary>1% of the containing query container's height.</summary>
        public Length Cqh => Length.Cqh(value);
        /// <summary>1% of the containing query container's inline size.</summary>
        public Length Cqi => Length.Cqi(value);
        /// <summary>1% of the containing query container's block size.</summary>
        public Length Cqb => Length.Cqb(value);
        /// <summary>1% of the smaller of <c>cqi</c> and <c>cqb</c>.</summary>
        public Length Cqmin => Length.Cqmin(value);
        /// <summary>1% of the larger of <c>cqi</c> and <c>cqb</c>.</summary>
        public Length Cqmax => Length.Cqmax(value);

        // ─── Duration / Angle / Percentage / Flex ───────────────────────────────
        /// <summary>Milliseconds.</summary>
        public Duration   Ms      => Duration.Ms(value);
        /// <summary>Degrees of angle.</summary>
        public Angle      Deg     => Angle.Deg(value);
        /// <summary>CSS percentage.</summary>
        public Percentage Percent => Percentage.Of(value);
        /// <summary>Grid fractional unit (<c>fr</c>).</summary>
        public Flex       Fr      => Flex.Fr(value);
    }

    extension(double value) {
        // ─── Length — absolute, font-relative, viewport-relative ────────────────
        /// <summary>Pixels — absolute length.</summary>
        public Length Px  => Length.Px(value);
        /// <summary>Em units — relative to the element's font size.</summary>
        public Length Em  => Length.Em(value);
        /// <summary>Root-em units — relative to the root element's font size.</summary>
        public Length Rem => Length.Rem(value);
        /// <summary>Viewport height percentage.</summary>
        public Length Vh  => Length.Vh(value);
        /// <summary>Viewport width percentage.</summary>
        public Length Vw  => Length.Vw(value);

        // ─── Length — container-query units ─────────────────────────────────────
        /// <summary>1% of the containing query container's width.</summary>
        public Length Cqw => Length.Cqw(value);
        /// <summary>1% of the containing query container's height.</summary>
        public Length Cqh => Length.Cqh(value);
        /// <summary>1% of the containing query container's inline size.</summary>
        public Length Cqi => Length.Cqi(value);
        /// <summary>1% of the containing query container's block size.</summary>
        public Length Cqb => Length.Cqb(value);
        /// <summary>1% of the smaller of <c>cqi</c> and <c>cqb</c>.</summary>
        public Length Cqmin => Length.Cqmin(value);
        /// <summary>1% of the larger of <c>cqi</c> and <c>cqb</c>.</summary>
        public Length Cqmax => Length.Cqmax(value);

        // ─── Duration / Angle / Percentage / Flex ───────────────────────────────
        /// <summary>Milliseconds.</summary>
        public Duration   Ms      => Duration.Ms(value);
        /// <summary>Seconds.</summary>
        public Duration   S       => Duration.S(value);
        /// <summary>Degrees of angle.</summary>
        public Angle      Deg     => Angle.Deg(value);
        /// <summary>CSS percentage.</summary>
        public Percentage Percent => Percentage.Of(value);
        /// <summary>Grid fractional unit (<c>fr</c>).</summary>
        public Flex       Fr      => Flex.Fr(value);
    }
}
