/*
 * ============================================================================
 * MESSAGE VALIDATION ATTRIBUTES
 * ============================================================================
 * 
 * PURPOSE:
 * --------
 * Decorates message fields to enforce size limits during deserialization.
 * Prevents malformed/malicious data from causing buffer overflows or crashes.
 * 
 * USAGE:
 * ------
 * public struct MyMessage : INetworkMessage
 * {
 *     [MaxLength(64)]
 *     public string PlayerName;
 *     
 *     [MaxSize(1024)]
 *     public byte[] Data;
 * }
 * 
 * ============================================================================
 */

using System;

namespace Eraflo.Catalyst.Networking
{
    /// <summary>
    /// Limits the maximum length of a string field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class MaxLengthAttribute : Attribute
    {
        /// <summary>Maximum string length in characters.</summary>
        public int Length { get; }

        /// <summary>
        /// Creates a max length constraint.
        /// </summary>
        /// <param name="length">Maximum characters allowed.</param>
        public MaxLengthAttribute(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be positive", nameof(length));
            Length = length;
        }
    }

    /// <summary>
    /// Limits the maximum size of an array or collection field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class MaxSizeAttribute : Attribute
    {
        /// <summary>Maximum array/collection size.</summary>
        public int Size { get; }

        /// <summary>
        /// Creates a max size constraint.
        /// </summary>
        /// <param name="size">Maximum elements allowed.</param>
        public MaxSizeAttribute(int size)
        {
            if (size <= 0)
                throw new ArgumentException("Size must be positive", nameof(size));
            Size = size;
        }
    }

    /// <summary>
    /// Marks a message as requiring validation before processing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false)]
    public class ValidateMessageAttribute : Attribute
    {
        /// <summary>Whether to reject invalid messages (vs just logging).</summary>
        public bool RejectInvalid { get; set; } = true;
    }
}
