﻿using System;
using System.Threading.Tasks;
using Utilities.Conversions;

namespace SqlLite.Wrapper.Serialization
{
	public class SqlSerializer<TDeserialized, TSerialized> : ISqlSerializer
	{
		public Type DeserializedType => typeof(TDeserialized);
		public Type SerializedType => typeof(TSerialized);

		protected virtual bool CanSerializeNull => !DeserializedType.IsValueType;
		protected virtual bool CanDeserializeNull => !SerializedType.IsValueType;

		public SqlSerializer() { }

		private SqlSerializerInvalidTypeException ReturningInvalidType(Type type)
		{
			return new SqlSerializerInvalidTypeException(GetType(), SerializedType, type,
				"returning value after serialization");
		}
		private SqlSerializerInvalidTypeException SerializingInvalidType(object input)
		{
			return new SqlSerializerInvalidTypeException
				(GetType(), DeserializedType, input?.GetType(), "serializing");
		}
		private SqlSerializerInvalidTypeException DeserializingInvalidType(object value)
		{
			return new SqlSerializerInvalidTypeException
				(GetType(), SerializedType, value.GetType(), "deserializing");
		}

		public object SerializeObject(object input)
		{
			object value = input switch
			{
				TDeserialized _t => Serialize(_t),
				null when CanSerializeNull => SerializeNull(),
				_ => throw SerializingInvalidType(input)
			};

			return VerifySerializedValue(value);
		}
		public async Task<object> SerializeObjectAsync(object input)
		{
			object value = input switch
			{
				TDeserialized _t => await SerializeAsync(_t),
				null when CanSerializeNull => SerializeNull(),
				_ => throw SerializingInvalidType(input)
			};

			return VerifySerializedValue(value);
		}

		private object VerifySerializedValue(object value)
		{
			Type type = value?.GetType();
			if (type != SerializedType && (type != null || !CanSerializeNull))
			{
				throw ReturningInvalidType(type);
			}

			return value;
		}

		protected virtual TSerialized SerializeNull() => default;
		protected virtual TSerialized Serialize(TDeserialized input)
		{
			if (!input.TryConvertTo(out TSerialized r))
			{
				throw new SqlSerializerFailedConversionException(
					GetType(), SerializedType, input.GetType(), "ConvertibleUtils.TryConvertTo");
			}
			
			return r;
		}
		protected virtual Task<TSerialized> SerializeAsync(TDeserialized input)
		{
			return Task.FromResult(Serialize(input));
		}

		public object DeserializeObject(object value)
		{
			return value switch
			{
				DBNull or null when CanDeserializeNull => DeserializeNull(),
				TSerialized _s => Deserialize(_s),
				_ => throw DeserializingInvalidType(value)
			};
		}
		public async Task<object> DeserializeObjectAsync(object value)
		{
			return value switch
			{
				DBNull or null when CanDeserializeNull => DeserializeNull(),
				TSerialized _s => await DeserializeAsync(_s),
				_ => throw DeserializingInvalidType(value)
			};
		}

		protected virtual TDeserialized DeserializeNull() => default;
		protected virtual TDeserialized Deserialize(TSerialized value)
		{
			if (!value.TryConvertTo(out TDeserialized r))
			{
				throw new SqlSerializerFailedConversionException(
					GetType(), DeserializedType, value.GetType(), "ConvertibleUtils.TryConvertTo");
			}

			return r;
		}
		protected virtual Task<TDeserialized> DeserializeAsync(TSerialized value)
		{
			return Task.FromResult(Deserialize(value));
		}
	}
}
