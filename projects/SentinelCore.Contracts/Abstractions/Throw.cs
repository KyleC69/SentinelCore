// Solution: SentinelCore
// Project:   SentinelCore.Contracts
// File:         Throw.cs
// Author: Kyle L. Crowder
// Build Num:  082808



using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;




// ReSharper disable once CheckNamespace
namespace SentinelCore.Abstractions;





//
// Summary:
//     Defines static methods used to throw exceptions.
//
// Remarks:
//     The main purpose is to reduce code size, improve performance, and standardize
//     exception messages.
[ExcludeFromCodeCoverage]
public static class Throw
{

    //
    // Summary:
    //     Throws an System.ArgumentException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    //
    //   message:
    //     A message that describes the error.
    [DoesNotReturn]
    public static void ArgumentException(string paramName, string? message)
    {
        throw new ArgumentException(message, paramName);
    }








    //
    // Summary:
    //     Throws an System.ArgumentException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    //
    //   message:
    //     A message that describes the error.
    //
    //   innerException:
    //     The exception that is the cause of the current exception.
    //
    // Remarks:
    //     If the innerException is not a null, the current exception is raised in a catch
    //     block that handles the inner exception.
    [DoesNotReturn]
    public static void ArgumentException(string paramName, string? message, Exception? innerException)
    {
        throw new ArgumentException(message, paramName, innerException);
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    [DoesNotReturn]
    public static void ArgumentNullException(string paramName)
    {
        throw new ArgumentNullException(paramName);
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    //
    //   message:
    //     A message that describes the error.
    [DoesNotReturn]
    public static void ArgumentNullException(string paramName, string? message)
    {
        throw new ArgumentNullException(paramName, message);
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    [DoesNotReturn]
    public static void ArgumentOutOfRangeException(string paramName)
    {
        throw new ArgumentOutOfRangeException(paramName);
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    //
    //   message:
    //     A message that describes the error.
    [DoesNotReturn]
    public static void ArgumentOutOfRangeException(string paramName, string? message)
    {
        throw new ArgumentOutOfRangeException(paramName, message);
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException.
    //
    // Parameters:
    //   paramName:
    //     The name of the parameter that caused the exception.
    //
    //   actualValue:
    //     The value of the argument that caused this exception.
    //
    //   message:
    //     A message that describes the error.
    [DoesNotReturn]
    public static void ArgumentOutOfRangeException(string paramName, object? actualValue, string? message)
    {
        throw new ArgumentOutOfRangeException(paramName, actualValue, message);
    }








    //
    // Summary:
    //     Throws an exception indicating that a required service is not available.
    public static InvalidOperationException CreateMissingServiceException(Type serviceType, object? serviceKey)
    {
        return new InvalidOperationException(serviceKey == null ? $"No service of type '{serviceType}' is available." : $"No service of type '{serviceType}' for the key '{serviceKey}' is available.");
    }








    //
    // Summary:
    //     Throws an System.ArgumentException if the argument's buffer size is less than
    //     the required buffer size.
    //
    // Parameters:
    //   bufferSize:
    //     The actual buffer size.
    //
    //   requiredSize:
    //     The required buffer size.
    //
    //   paramName:
    //     The name of the parameter to be checked.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IfBufferTooSmall(int bufferSize, int requiredSize, string paramName = "")
    {
        if (bufferSize < requiredSize)
            ArgumentException(paramName, $"Buffer too small, needed a size of {requiredSize} but got {bufferSize}");
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater than max.
    //
    //   max:
    //     The number that must be greater than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfGreaterThan(int argument, int max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater than max.
    //
    //   max:
    //     The number that must be greater than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfGreaterThan(uint argument, uint max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater than max.
    //
    //   max:
    //     The number that must be greater than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfGreaterThan(long argument, long max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater than max.
    //
    //   max:
    //     The number that must be greater than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfGreaterThan(ulong argument, ulong max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater than max.
    //
    //   max:
    //     The number that must be greater than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfGreaterThan(double argument, double max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (!(argument <= max))
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     or equal than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   max:
    //     The number that must be greater or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfGreaterThanOrEqual(int argument, int max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument >= max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater or equal than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     or equal than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   max:
    //     The number that must be greater or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfGreaterThanOrEqual(uint argument, uint max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument >= max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater or equal than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     or equal than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   max:
    //     The number that must be greater or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfGreaterThanOrEqual(long argument, long max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument >= max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater or equal than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     or equal than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   max:
    //     The number that must be greater or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfGreaterThanOrEqual(ulong argument, ulong max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument >= max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater or equal than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is greater
    //     or equal than max.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   max:
    //     The number that must be greater or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfGreaterThanOrEqual(double argument, double max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (!(argument < max))
            ArgumentOutOfRangeException(paramName, argument, $"Argument greater or equal than maximum value {max}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less than min.
    //
    //   min:
    //     The number that must be less than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfLessThan(int argument, int min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min) ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less than min.
    //
    //   min:
    //     The number that must be less than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfLessThan(uint argument, uint min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min) ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less than min.
    //
    //   min:
    //     The number that must be less than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfLessThan(long argument, long min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min) ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less than min.
    //
    //   min:
    //     The number that must be less than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfLessThan(ulong argument, ulong min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min) ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less than min.
    //
    //   min:
    //     The number that must be less than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfLessThan(double argument, double min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (!(argument >= min))
            ArgumentOutOfRangeException(paramName, argument, $"Argument less than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     or equal than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less or equal than min.
    //
    //   min:
    //     The number that must be less or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfLessThanOrEqual(int argument, int min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument <= min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less or equal than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     or equal than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less or equal than min.
    //
    //   min:
    //     The number that must be less or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfLessThanOrEqual(uint argument, uint min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument <= min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less or equal than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     or equal than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less or equal than min.
    //
    //   min:
    //     The number that must be less or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfLessThanOrEqual(long argument, long min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument <= min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less or equal than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     or equal than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less or equal than min.
    //
    //   min:
    //     The number that must be less or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfLessThanOrEqual(ulong argument, ulong min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument <= min)
            ArgumentOutOfRangeException(paramName, argument, $"Argument less or equal than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is less
    //     or equal than min.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being less or equal than min.
    //
    //   min:
    //     The number that must be less or equal than the argument.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfLessThanOrEqual(double argument, double min, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (!(argument > min))
            ArgumentOutOfRangeException(paramName, argument, $"Argument less or equal than minimum value {min}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentException if the specified member is null.
    //
    // Parameters:
    //   argument:
    //     Argument to which member belongs.
    //
    //   member:
    //     Object member to be checked for null.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    //   memberName:
    //     The name of the member.
    //
    // Type parameters:
    //   TParameter:
    //     Argument type.
    //
    //   TMember:
    //     Member type to be checked for null.
    //
    // Returns:
    //     The original value of member.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static TMember IfMemberNull<TParameter, TMember>(TParameter argument, [NotNull] TMember member, [CallerArgumentExpression("argument")] string paramName = "", [CallerArgumentExpression("member")] string memberName = "") where TParameter : notnull
    {
        if (member == null) ArgumentException(paramName, $"Member {memberName} of {paramName} is null");

        return member;
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException if the specified argument is null.
    //
    // Parameters:
    //   argument:
    //     Object to be checked for null.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Type parameters:
    //   T:
    //     Argument type to be checked for null.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static T IfNull<T>([NotNull] T argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == null) ArgumentNullException(paramName);

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException if the string is null, or System.ArgumentException
    //     if it is empty.
    //
    // Parameters:
    //   argument:
    //     String to be checked for null or empty.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string IfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (string.IsNullOrEmpty(argument))
        {
            if (argument == null)
                ArgumentNullException(paramName);
            else
                ArgumentException(paramName, "Argument is an empty string");
        }

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException if the collection is null, or System.ArgumentException
    //     if it is empty.
    //
    // Parameters:
    //   argument:
    //     The collection to evaluate.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Type parameters:
    //   T:
    //     The type of objects in the collection.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [ExcludeFromCodeCoverage]
    [return: NotNull]
    public static IEnumerable<T> IfNullOrEmpty<T>([NotNull] IEnumerable<T>? argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == null)
        {
            ArgumentNullException(paramName);
        }
        else if (!(argument is ICollection<T> collection))
        {
            if (argument is IReadOnlyCollection<T> readOnlyCollection)
            {
                if (readOnlyCollection.Count == 0) ArgumentException(paramName, "Collection is empty");
            }
            else
            {
                using var enumerator = argument.GetEnumerator();
                if (!enumerator.MoveNext()) ArgumentException(paramName, "Collection is empty");
            }
        }
        else if (collection.Count == 0)
        {
            ArgumentException(paramName, "Collection is empty");
        }

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentNullException if the specified argument is null, or
    //     System.ArgumentException if the specified member is null.
    //
    // Parameters:
    //   argument:
    //     Argument to be checked for null.
    //
    //   member:
    //     Object member to be checked for null.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    //   memberName:
    //     The name of the member.
    //
    // Type parameters:
    //   TParameter:
    //     Argument type to be checked for null.
    //
    //   TMember:
    //     Member type to be checked for null.
    //
    // Returns:
    //     The original value of member.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static TMember IfNullOrMemberNull<TParameter, TMember>([NotNull] TParameter argument, [NotNull] TMember member, [CallerArgumentExpression("argument")] string paramName = "", [CallerArgumentExpression("member")] string memberName = "")
    {
        if (argument == null) ArgumentNullException(paramName);

        if (member == null) ArgumentException(paramName, $"Member {memberName} of {paramName} is null");

        return member;
    }








    //
    // Summary:
    //     Throws either an System.ArgumentNullException or an System.ArgumentException
    //     if the specified string is null or whitespace respectively.
    //
    // Parameters:
    //   argument:
    //     String to be checked for null or whitespace.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNull]
    public static string IfNullOrWhitespace([NotNull] string? argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            if (argument == null)
                ArgumentNullException(paramName);
            else
                ArgumentException(paramName, "Argument is whitespace");
        }

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the enum value is not valid.
    //
    //
    // Parameters:
    //   argument:
    //     The argument to evaluate.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Type parameters:
    //   T:
    //     The type of the enumeration.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T IfOutOfRange<T>(T argument, [CallerArgumentExpression("argument")] string paramName = "") where T : struct, Enum
    {
        if (!Enum.IsDefined(argument))
            ArgumentOutOfRangeException(paramName, $"{argument} is an invalid value for enum type {typeof(T)}");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is not in
    //     the specified range.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   min:
    //     The lower bound of the allowed range of argument values.
    //
    //   max:
    //     The upper bound of the allowed range of argument values.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfOutOfRange(int argument, int min, int max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min || argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is not in
    //     the specified range.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   min:
    //     The lower bound of the allowed range of argument values.
    //
    //   max:
    //     The upper bound of the allowed range of argument values.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfOutOfRange(uint argument, uint min, uint max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min || argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is not in
    //     the specified range.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   min:
    //     The lower bound of the allowed range of argument values.
    //
    //   max:
    //     The upper bound of the allowed range of argument values.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfOutOfRange(long argument, long min, long max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min || argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is not in
    //     the specified range.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   min:
    //     The lower bound of the allowed range of argument values.
    //
    //   max:
    //     The upper bound of the allowed range of argument values.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfOutOfRange(ulong argument, ulong min, ulong max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument < min || argument > max)
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is not in
    //     the specified range.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being greater or equal than max.
    //
    //   min:
    //     The lower bound of the allowed range of argument values.
    //
    //   max:
    //     The upper bound of the allowed range of argument values.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfOutOfRange(double argument, double min, double max, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (!(min <= argument) || !(argument <= max))
            ArgumentOutOfRangeException(paramName, argument, $"Argument not in the range [{min}..{max}]");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is equal
    //     to 0.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being not equal to zero.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IfZero(int argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == 0) ArgumentOutOfRangeException(paramName, "Argument is zero");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is equal
    //     to 0.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being not equal to zero.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IfZero(uint argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == 0) ArgumentOutOfRangeException(paramName, "Argument is zero");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is equal
    //     to 0.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being not equal to zero.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long IfZero(long argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == 0L) ArgumentOutOfRangeException(paramName, "Argument is zero");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is equal
    //     to 0.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being not equal to zero.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong IfZero(ulong argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == 0L) ArgumentOutOfRangeException(paramName, "Argument is zero");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.ArgumentOutOfRangeException if the specified number is equal
    //     to 0.
    //
    // Parameters:
    //   argument:
    //     Number to be expected being not equal to zero.
    //
    //   paramName:
    //     The name of the parameter being checked.
    //
    // Returns:
    //     The original value of argument.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double IfZero(double argument, [CallerArgumentExpression("argument")] string paramName = "")
    {
        if (argument == 0.0) ArgumentOutOfRangeException(paramName, "Argument is zero");

        return argument;
    }








    //
    // Summary:
    //     Throws an System.InvalidOperationException.
    //
    // Parameters:
    //   message:
    //     A message that describes the error.
    [DoesNotReturn]
    public static void InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }








    //
    // Summary:
    //     Throws an System.InvalidOperationException.
    //
    // Parameters:
    //   message:
    //     A message that describes the error.
    //
    //   innerException:
    //     The exception that is the cause of the current exception.
    [DoesNotReturn]
    public static void InvalidOperationException(string message, Exception? innerException)
    {
        throw new InvalidOperationException(message, innerException);
    }
}
#if false // Decompilation log
'273' items in cache
------------------
Resolve: 'System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Runtime.dll'
------------------
Resolve: 'System.Collections, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Collections.dll'
------------------
Resolve: 'System.Text.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Text.Json, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Text.Json.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'System.Memory, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Memory, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Memory.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Net.Http, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Net.Http.dll'
------------------
Resolve: 'System.ComponentModel, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.ComponentModel.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.ComponentModel.Annotations, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Annotations, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.ComponentModel.Annotations.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Linq, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Linq.dll'
------------------
Resolve: 'System.Text.Encodings.Web, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Found single assembly: 'System.Text.Encodings.Web, Version=10.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Text.Encodings.Web.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=10.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.10\ref\net10.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif