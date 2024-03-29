﻿using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace SqlLite.Wrapper
{
	public partial class SqliteHandler
	{
		public async Task<int> SaveManyAsync<T, I>(IEnumerable<T> entries, Action<T, int> onIterate = null)
			where T : ISqlTable<I>
		{
			using SqliteContext context = await CreateContext().OpenAsync();
			object _target = null;
			try
			{
				Type type = typeof(T);
				TableInfo table = await GetTableInfoAsync(type);

				int? aId = table.idAutoIncr == null ? null : await table.idAutoIncr.GetNextIndexAsync(this);

				SqliteCommand command = context.CreateCommand(table.GetSaveQuery(entries.Count()));

				TableMember[] fields = table.fields;
				int index = 0;
				string ParameterName(TableMember member)
				{
					string name = member.Name;
					if (index > 0)
						name += index;
					return name;
				}

				foreach (T entry in entries)
				{
					//Force async not to block process.
					await Task.Yield();
					_target = entry;

					if (aId is not null)
					{
						entry.Id = (I)(object)aId;
						aId++;
					}

					object id = await table.identifier.GetValueAsync(this, entry);
					command.Parameters.AddWithValue(ParameterName(table.identifier), id);

					for (int i = 0; i < fields.Length; i++)
					{
						TableMember field = fields[i];
						object v = await field.GetValueAsync(this, entry);
						command.Parameters.AddWithValue(ParameterName(field), v);
					}

					onIterate?.Invoke(entry, index);
					index++;
				}

				int operations = await command.ExecuteNonQueryAsync();
				OnCommandExecuted(command, operations, entries);

				if (table.idAutoIncr != null)
					await table.idAutoIncr.SetNextIndexAsync(this, aId.Value);

				return operations;
			}
			catch (Exception e)	
			{
				OnException(e, context, _target);
				throw e;
			}
		}

		public async Task<int> SaveAsync<T>(ISqlTable<T> entry)
		{
			using SqliteContext context = await CreateContext().OpenAsync();
			object _target = entry;
			try
			{
				Type type = entry.GetType();
				TableInfo table = await GetTableInfoAsync(type);

				if (table.idAutoIncr != null && entry.Id.Equals(default))
					await table.identifier.SetValueAsync(this, entry, await table.idAutoIncr.GetNextIndexAsync(this));

				SqliteCommand command = context.CreateCommand(table.SaveQuery);
				command.Parameters.Add(await table.identifier.GetParameterAsync(this, entry));

				TableMember[] fields = table.fields;

				for (int i = 0; i < fields.Length; i++)
					command.Parameters.Add(await fields[i].GetParameterAsync(this, entry));

				int ops = await command.ExecuteNonQueryAsync();
				OnCommandExecuted(command, ops, _target);
				return ops;
			}
			catch (Exception e)
			{
				OnException(e, context, _target);
				throw e;
			}
		}

		public int SaveMany<T, I>(IEnumerable<T> entries, Action<T, int> onSave = null)
			where T : ISqlTable<I>
		{
			using SqliteContext context = CreateContext().Open();
			object _target = null;
			try
			{
				Type type = typeof(T);
				TableInfo table = GetTableInfo(type);

				int? aId = table.idAutoIncr?.GetNextIndex(this);

				SqliteCommand command = context.CreateCommand(table.GetSaveQuery(entries.Count()));

				command.Parameters.AddWithValue(table.identifier.Name, null);
				TableMember[] fields = table.fields;
				for (int i = 0; i < fields.Length; i++)
					command.Parameters.AddWithValue(fields[i].Name, null);

				int index = 0;
				string ParameterName(TableMember member)
				{
					string name = member.Name;
					if (index > 0)
						name += index;
					return name;
				}

				foreach (T entry in entries)
				{
					_target = entry;

					if (aId is not null)
					{
						entry.Id = (I)(object)aId;
						aId++;
					}

					object id = table.identifier.GetValue(this, entry);
					command.Parameters.AddWithValue(ParameterName(table.identifier), id);

					for (int i = 0; i < fields.Length; i++)
					{
						TableMember field = fields[i];
						object v = field.GetValue(this, entry);
						command.Parameters.AddWithValue(ParameterName(field), v);
					}

					onSave?.Invoke(entry, index);
					index++;
				}

				int operations = command.ExecuteNonQuery();
				OnCommandExecuted(command, operations, entries);

				if (table.idAutoIncr != null)
					table.idAutoIncr.SetNextIndex(this, aId.Value);

				return operations;
			}
			catch (Exception e)
			{
				OnException(e, context, _target);
				throw e;
			}
		}

		public int Save<T>(ISqlTable<T> entry)
		{
			using SqliteContext context = CreateContext().Open();
			object _target = entry;
			try
			{
				Type type = entry.GetType();
				TableInfo table = GetTableInfo(type);

				if (table.idAutoIncr != null && entry.Id.Equals(default))
					table.identifier.SetValue(this, entry, table.idAutoIncr.GetNextIndex(this));

				SqliteCommand command = context.CreateCommand(table.SaveQuery);
				command.Parameters.Add(table.identifier.GetParameter(this, entry));

				TableMember[] fields = table.fields;

				for (int i = 0; i < fields.Length; i++)
					command.Parameters.Add(fields[i].GetParameter(this, entry));

				int ops = command.ExecuteNonQuery();
				OnCommandExecuted(command, ops, _target);
				return ops;
			}
			catch (Exception e)
			{
				OnException(e, context, _target);
				throw e;
			}
		}
	}
}
