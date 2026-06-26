using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Overfloat;

public static unsafe partial class OverfloatExports
{
    [UnmanagedCallersOnly(EntryPoint = "overfloat_version_major")]
    public static int VersionMajor()
        => OverfloatInfo.MajorVersion;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_version_minor")]
    public static int VersionMinor()
        => OverfloatInfo.MinorVersion;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_version_patch")]
    public static int VersionPatch()
        => OverfloatInfo.PatchVersion;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_exception_flags_get")]
    public static int ExceptionFlagsGet()
        => (int)OverfloatEnvironment.ExceptionFlags;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_exception_flags_clear")]
    public static void ExceptionFlagsClear()
        => OverfloatEnvironment.ClearExceptionFlags();

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_create")]
    public static nint SpecCreate(int exponentBits, int mantissaBits, int roundingMode)
    {
        if (!Enum.IsDefined(typeof(OverfloatRoundingMode), roundingMode))
        {
            return nint.Zero;
        }

        try
        {
            var spec = new OverfloatSpecification(exponentBits, mantissaBits, (OverfloatRoundingMode)roundingMode);
            return OverfloatHandleHelpers.Allocate(spec);
        }
        catch (ArgumentOutOfRangeException)
        {
            return nint.Zero;
        }
        catch (OverflowException)
        {
            return nint.Zero;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_create_from_total_bits")]
    public static nint SpecCreateFromTotalBits(int totalBits, int roundingMode)
    {
        if (!Enum.IsDefined(typeof(OverfloatRoundingMode), roundingMode))
        {
            return nint.Zero;
        }

        try
        {
            var spec = OverfloatSpecification.FromTotalBits(totalBits, (OverfloatRoundingMode)roundingMode);
            return OverfloatHandleHelpers.Allocate(spec);
        }
        catch (ArgumentException)
        {
            return nint.Zero;
        }
        catch (OverflowException)
        {
            return nint.Zero;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_free")]
    public static void SpecFree(nint specHandle)
        => OverfloatHandleHelpers.Free(specHandle);

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_exponent_bits")]
    public static int SpecExponentBits(nint specHandle)
        => OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle)?.ExponentBits ?? -1;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_mantissa_bits")]
    public static int SpecMantissaBits(nint specHandle)
        => OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle)?.MantissaBits ?? -1;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_rounding_mode")]
    public static int SpecRoundingMode(nint specHandle)
    {
        var specification = OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle);
        return specification is null ? -1 : (int)specification.RoundingMode;
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_spec_validate")]
    public static int SpecValidate(nint specHandle)
    {
        var specification = OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle);
        return specification?.Validate() == OverfloatStatus.Success ? 0 : 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_parse")]
    public static nint NumberParse(nint specHandle, byte* text)
    {
        var specification = OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle);
        if (specification is null)
        {
            return nint.Zero;
        }

        var input = Utf8Helpers.ReadNullTerminated(text);
        if (string.IsNullOrWhiteSpace(input))
        {
            return nint.Zero;
        }

        try
        {
            var value = OverfloatParsing.Parse(specification, input);
            return OverfloatHandleHelpers.Allocate(value);
        }
        catch (FormatException)
        {
            return nint.Zero;
        }
        catch (OverflowException)
        {
            return nint.Zero;
        }
        catch (NotSupportedException)
        {
            return nint.Zero;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_from_bits_hex")]
    public static nint NumberFromBitsHex(nint specHandle, byte* text)
    {
        var specification = OverfloatHandleHelpers.Get<OverfloatSpecification>(specHandle);
        if (specification is null)
        {
            return nint.Zero;
        }

        var input = Utf8Helpers.ReadNullTerminated(text);
        if (string.IsNullOrWhiteSpace(input))
        {
            return nint.Zero;
        }

        try
        {
            var value = OverfloatBitConverter.FromHexString(specification, input);
            return OverfloatHandleHelpers.Allocate(value);
        }
        catch (FormatException)
        {
            return nint.Zero;
        }
        catch (OverflowException)
        {
            return nint.Zero;
        }
        catch (NotSupportedException)
        {
            return nint.Zero;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_free")]
    public static void NumberFree(nint numberHandle)
        => OverfloatHandleHelpers.Free(numberHandle);

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_add")]
    public static nint NumberAdd(nint leftHandle, nint rightHandle)
        => BinaryOp(leftHandle, rightHandle, static (left, right) => OverfloatMath.Add(left, right));

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_subtract")]
    public static nint NumberSubtract(nint leftHandle, nint rightHandle)
        => BinaryOp(leftHandle, rightHandle, static (left, right) => OverfloatMath.Subtract(left, right));

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_multiply")]
    public static nint NumberMultiply(nint leftHandle, nint rightHandle)
        => BinaryOp(leftHandle, rightHandle, static (left, right) => OverfloatMath.Multiply(left, right));

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_divide")]
    public static nint NumberDivide(nint leftHandle, nint rightHandle)
        => BinaryOp(leftHandle, rightHandle, static (left, right) => OverfloatMath.Divide(left, right));

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_compare")]
    public static int NumberCompare(nint leftHandle, nint rightHandle)
    {
        var left = OverfloatHandleHelpers.Get<OverfloatNumber>(leftHandle);
        var right = OverfloatHandleHelpers.Get<OverfloatNumber>(rightHandle);
        if (left is null || right is null)
        {
            return int.MinValue;
        }

        try
        {
            return OverfloatMath.Compare(left, right);
        }
        catch (InvalidOperationException)
        {
            return 2;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_compare_total")]
    public static int NumberCompareTotal(nint leftHandle, nint rightHandle)
    {
        var left = OverfloatHandleHelpers.Get<OverfloatNumber>(leftHandle);
        var right = OverfloatHandleHelpers.Get<OverfloatNumber>(rightHandle);
        if (left is null || right is null)
        {
            return int.MinValue;
        }

        try
        {
            return OverfloatMath.CompareTotal(left, right);
        }
        catch (InvalidOperationException)
        {
            return int.MinValue;
        }
        catch (NotSupportedException)
        {
            return int.MinValue;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_classification")]
    public static int NumberClassification(nint numberHandle)
        => (int?)(OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle)?.Classification) ?? -1;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_is_negative")]
    public static int NumberIsNegative(nint numberHandle)
        => OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle)?.Negative == true ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_is_signaling_nan")]
    public static int NumberIsSignalingNaN(nint numberHandle)
        => OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle)?.IsSignalingNaN == true ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_binary_exponent")]
    public static int NumberBinaryExponent(nint numberHandle)
        => OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle)?.BinaryExponent ?? 0;

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_format")]
    public static int NumberFormat(nint numberHandle, byte* buffer, int bufferLength)
    {
        var number = OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle);
        if (number is null)
        {
            return 0;
        }

        return Utf8Helpers.WriteNullTerminated(number.ToString(), buffer, bufferLength);
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_nan_payload_format")]
    public static int NumberNaNPayloadFormat(nint numberHandle, byte* buffer, int bufferLength)
    {
        var number = OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle);
        if (number is null || number.Classification != OverfloatClassification.NaN)
        {
            return 0;
        }

        return Utf8Helpers.WriteNullTerminated(number.NaNPayload.ToString(), buffer, bufferLength);
    }

    [UnmanagedCallersOnly(EntryPoint = "overfloat_number_to_bits_hex")]
    public static int NumberToBitsHex(nint numberHandle, byte* buffer, int bufferLength)
    {
        var number = OverfloatHandleHelpers.Get<OverfloatNumber>(numberHandle);
        if (number is null)
        {
            return 0;
        }

        return Utf8Helpers.WriteNullTerminated(number.ToBitPatternHex(), buffer, bufferLength);
    }

    private static nint BinaryOp(nint leftHandle, nint rightHandle, Func<OverfloatNumber, OverfloatNumber, OverfloatNumber> operation)
    {
        var left = OverfloatHandleHelpers.Get<OverfloatNumber>(leftHandle);
        var right = OverfloatHandleHelpers.Get<OverfloatNumber>(rightHandle);
        if (left is null || right is null)
        {
            return nint.Zero;
        }

        try
        {
            return OverfloatHandleHelpers.Allocate(operation(left, right));
        }
        catch (InvalidOperationException)
        {
            return nint.Zero;
        }
        catch (NotSupportedException)
        {
            return nint.Zero;
        }
    }
}
