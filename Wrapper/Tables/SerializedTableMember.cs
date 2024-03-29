﻿using SqlLite.Wrapper.Serialization;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace SqlLite.Wrapper
{
	public partial class SqliteHandler
	{
		private class SerializedTableMember : TableMember
		{
			private readonly SqlSerializerAttribute attribute;
			public override bool IsForeign => true;
			public override Type SerializedType => attribute.Serializer.SerializedType;

			public SerializedTableMember(MemberInfo member, SqlSerializerAttribute attribute) : base(member)
			{
				this.attribute = attribute;
			}

			public override object GetValue(SqliteHandler context, object instance)
			{
				ISqlSerializer serializer = attribute.Serializer;
				object value = base.GetValue(context, instance);
				return serializer.SerializeObject(value);
			}

			public override async Task<object> GetValueAsync(SqliteHandler context, object instance)
			{
				ISqlSerializer serializer = attribute.Serializer;
				object value = base.GetValue(context, instance);
				return await serializer.SerializeObjectAsync(value);
			}

			public override void SetValue(SqliteHandler context, object instance, object value)
			{
				ISqlSerializer serializer = attribute.Serializer;
				value = serializer.DeserializeObject(value);
				base.SetValue(context, instance, value);
			}

			public override async Task SetValueAsync(SqliteHandler context, object instance, object value)
			{
				ISqlSerializer serializer = attribute.Serializer;
				value = await serializer.DeserializeObjectAsync(value);
				base.SetValue(context, instance, value);
			}
		}
	}
}
