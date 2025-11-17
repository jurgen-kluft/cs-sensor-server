// Auto-generated from Go source: Full 1-to-1 conversion with IsFieldName and escape handling
using System;
using System.Collections.Generic;
using System.Text;

namespace CorePkg
{
	public enum JsonValueType : sbyte
	{
		JsonValueTypeError = -1,
		JsonValueTypeEmpty = 0,
		JsonValueTypeObject = 1,
		JsonValueTypeArray = 2,
		JsonValueTypeString = 3,
		JsonValueTypeNumber = 4,
		JsonValueTypeBool = 5,
		JsonValueTypeNull = 6,
		JsonValueTypeEnd = 7
	}

	public static class JsonValueTypeExtensions
	{
		private static readonly Dictionary<JsonValueType, string> JsonValueTypeStringMap = new()
		{
		{ JsonValueType.JsonValueTypeError,  "Error" },
		{ JsonValueType.JsonValueTypeEmpty,  "Empty" },
		{ JsonValueType.JsonValueTypeObject, "Object" },
		{ JsonValueType.JsonValueTypeArray,  "Array" },
		{ JsonValueType.JsonValueTypeString, "String" },
		{ JsonValueType.JsonValueTypeNumber, "Number" },
		{ JsonValueType.JsonValueTypeBool,   "Bool" },
		{ JsonValueType.JsonValueTypeNull,   "Null" },
		{ JsonValueType.JsonValueTypeEnd,    "End" }
	};

		public static string ToFriendlyString(this JsonValueType valueType)
		{
			return JsonValueTypeStringMap.TryGetValue(valueType, out var str) ? str : "Unknown";
		}
	}

	public enum JsonResult : sbyte
	{
		JsonResultError = -1,
		JsonResultEmpty = 0,
		JsonResultObject = 1,
		JsonResultArray = 2,
		JsonResultString = 3,
		JsonResultNumber = 4,
		JsonResultBool = 5,
		JsonResultNull = 6,
		JsonResultEnd = 7,
		JsonResultOk = 8
	}

	public static class JsonResultExtensions
	{
		private static readonly Dictionary<JsonResult, string> JsonResultStringMap = new()
		{
		{ JsonResult.JsonResultError,  "Error" },
		{ JsonResult.JsonResultEmpty,  "Empty" },
		{ JsonResult.JsonResultObject, "Object" },
		{ JsonResult.JsonResultArray,  "Array" },
		{ JsonResult.JsonResultString, "String" },
		{ JsonResult.JsonResultNumber, "Number" },
		{ JsonResult.JsonResultBool,   "Bool" },
		{ JsonResult.JsonResultNull,   "Null" },
		{ JsonResult.JsonResultEnd,    "End" }
	};

		public static string ToFriendlyString(this JsonResult result)
		{
			return JsonResultStringMap.TryGetValue(result, out var str) ? str : "Unknown";
		}
	}
	public class JsonField
	{
		public int Begin { get; set; }
		public short Length { get; set; }
		public JsonValueType Type { get; set; }
		public sbyte Padding { get; set; }

		public override string ToString()
		{
			return $"Field{{Begin: {Begin}, Length: {Length}, Type: {Type.ToFriendlyString()}}}";
		}

		public bool IsEmpty()
		{
			return Type == JsonValueType.JsonValueTypeEmpty || Length == 0;
		}

		public static readonly JsonField sEmpty = new()
		{
			Begin = 0,
			Length = 0,
			Type = JsonValueType.JsonValueTypeEmpty
		};

		public static readonly JsonField sError = new()
		{
			Begin = 0,
			Length = 0,
			Type = JsonValueType.JsonValueTypeError
		};

		public static JsonField NewJsonField(int begin, short length, JsonValueType valueType)
		{
			return new JsonField
			{
				Begin = begin,
				Length = length,
				Type = valueType
			};
		}
	}
	public class JsonContext
	{
		public string Json;
		public int JsonLen;
		public int Cursor;
		public bool IsEscapeString;
		public JsonValueType[] Stack;
		public short StackIndex;
		public StringBuilder EscapedStrings;
		private const short JsonStackSize = 256;

		public JsonContext()
		{
			Json = string.Empty;
			Cursor = 0;
			IsEscapeString = true;
			Stack = new JsonValueType[JsonStackSize];
			StackIndex = JsonStackSize;
			EscapedStrings = new StringBuilder();
		}

		public bool IsValidField(JsonField f) => f.Begin >= 0 && f.Length > 0 && (f.Begin + f.Length <= Json.Length);

		public string GetEscapedString(JsonField f)
		{
			EscapedStrings.Clear();
			int index = f.Begin;
			int end = f.Begin + f.Length;
			while (index < end)
			{
				char c = Json[index];
				if (c == '\\')
				{
					index++;
					if (index >= end) break;
					char esc = Json[index];
					switch (esc)
					{
						case '"': EscapedStrings.Append('"'); break;
						case '\\': EscapedStrings.Append('\\'); break;
						case '/': EscapedStrings.Append('/'); break;
						case 'b': EscapedStrings.Append('\b'); break;
						case 'f': EscapedStrings.Append('\f'); break;
						case 'n': EscapedStrings.Append('\n'); break;
						case 'r': EscapedStrings.Append('\r'); break;
						case 't': EscapedStrings.Append('\t'); break;
						case 'u':
							if (index + 4 < end)
							{
								string hex = Json.Substring(index + 1, 4);
								if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
								{
									EscapedStrings.Append((char)code);
								}
								index += 4;
							}
							break;
						default: EscapedStrings.Append(esc); break;
					}
				}
				else
				{
					EscapedStrings.Append(c);
				}
				index++;
			}
			return EscapedStrings.ToString();
		}
	}
	public partial class JsonDecoder
	{
		public JsonContext Context { get; set; }
		public JsonField Key { get; set; }
		public JsonField Value { get; set; }
		public Exception Error { get; set; }

		public JsonDecoder()
		{
			Context = new JsonContext();
			Key = JsonField.JsonFieldError;
			Value = JsonField.JsonFieldError;
			Error = null;
		}

		public static JsonDecoder NewJsonDecoder()
		{
			return new JsonDecoder();
		}

		public bool Begin(string json)
		{
			return Context.ParseBegin(json);
		}

		public void End()
		{
			Context.Json = string.Empty;
			Context.JsonLen = 0;
			Context.Cursor = 0;
			Context.StackIndex = JsonContext.JsonStackSize;
			Context.EscapedStrings.Clear();
			Key = JsonField.JsonFieldError;
			Value = JsonField.JsonFieldError;
			Error = null;
		}
	}

	public delegate void JsonDecode(JsonDecoder decoder);

	public partial class JsonDecoder
	{


		public Exception Decode(Dictionary<string, JsonDecode> fields)
		{
			var (ok, end) = ReadUntilObjectEnd();
			while (ok && !end)
			{
				string fname = DecodeField();
				if (fields.TryGetValue(fname.ToLowerInvariant(), out var fdecode))
				{
					fdecode(this);
				}
				(ok, end) = ReadUntilObjectEnd();
			}
			if (!ok && Error == null)
			{
				return new Exception($"error decoding JSON at {Context.Cursor}");
			}
			return Error;
		}

		public string DecodeField() => FieldStr(Key);
		public bool DecodeBool() => ParseBool(Value);
		public int DecodeInt32() => ParseInt32(Value);
		public long DecodeInt64() => ParseInt64(Value);
		public float DecodeFloat32() => ParseFloat32(Value);
		public double DecodeFloat64() => ParseFloat64(Value);
		public string DecodeString() => ParseString(Value);

		public List<string> DecodeStringArray()
		{
			var outArray = new List<string>(4);
			var (ok, end) = ReadUntilArrayEnd();
			while (ok && !end)
			{
				outArray.Add(DecodeString());
				(ok, end) = ReadUntilArrayEnd();
			}
			return outArray;
		}

		public void DecodeArray(Action<JsonDecoder> decodeElement)
		{
			var (ok, end) = ReadUntilArrayEnd();
			while (ok && !end)
			{
				decodeElement(this);
				(ok, end) = ReadUntilArrayEnd();
			}
		}

		public List<List<string>> DecodeStringArray2D()
		{
			var outArray = new List<List<string>>(4);
			var (ok, end) = ReadUntilArrayEnd();
			while (ok && !end)
			{
				outArray.Add(DecodeStringArray());
				(ok, end) = ReadUntilArrayEnd();
			}
			return outArray;
		}

		public Dictionary<string, string> DecodeStringMapString()
		{
			var outMap = new Dictionary<string, string>(4);
			var (ok, end) = ReadUntilObjectEnd();
			while (ok && !end)
			{
				string key = DecodeField();
				string value = DecodeString();
				outMap[key] = value;
				(ok, end) = ReadUntilObjectEnd();
			}
			return outMap;
		}

		public string FieldStr(JsonField f)
		{
			return Context.Json.Substring(f.Begin, f.Length);
		}

		public bool ParseBool(JsonField field)
		{
			if (Context.IsValidField(field))
			{
				try
				{
					return bool.Parse(FieldStr(field));
				}
				catch (Exception ex)
				{
					Error = ex;
				}
			}
			return false;
		}

		public float ParseFloat32(JsonField field)
		{
			if (Context.IsValidField(field))
			{
				try
				{
					string value = FieldStr(field);
					double r64 = double.Parse(value);
					return (float)r64;
				}
				catch (Exception ex)
				{
					Error = ex;
				}
			}
			return 0f;
		}

		public double ParseFloat64(JsonField field)
		{
			if (Context.IsValidField(field))
			{
				try
				{
					string valueStr = FieldStr(field);
					return double.Parse(valueStr);
				}
				catch (Exception ex)
				{
					Error = ex;
				}
			}
			else
			{
				Error = new Exception($"invalid '{field}'");
			}
			return 0d;
		}

		public int ParseInt32(JsonField field)
		{
			return (int)ParseInt64(field);
		}

		public long ParseInt64(JsonField field)
		{
			if (Context.IsValidField(field))
			{
				try
				{
					string valueStr = FieldStr(field);
					return long.Parse(valueStr);
				}
				catch (Exception ex)
				{
					Error = ex;
				}
			}
			else
			{
				Error = new Exception($"invalid '{field}'");
			}
			return 0L;
		}

		public string ParseString(JsonField field)
		{
			if (Context.IsEscapeString)
			{
				string result = Context.GetEscapedString(field);
				if (result == null)
				{
					Error = new Exception($"invalid string at {field}");
				}
				return result;
			}
			else
			{
				if (Context.IsValidField(field))
				{
					return FieldStr(field);
				}
			}
			return string.Empty;
		}

		public bool IsFieldName(JsonField field, string name)
		{
			if (Context.IsValidField(field))
			{
				string fieldName = FieldStr(field);
				return string.Equals(fieldName, name, StringComparison.OrdinalIgnoreCase);
			}
			else
			{
				Error = new Exception($"invalid '{field}'");
			}
			return false;
		}

		public (bool ok, bool end) ReadUntilObjectEnd()
		{
			bool ok = Read();
			if (!ok)
			{
				return (false, false);
			}
			if (Key.Type == JsonValueType.JsonValueTypeObject && Value.Type == JsonValueType.JsonValueTypeEnd)
			{
				return (true, true);
			}
			return (true, false);
		}

		public (bool ok, bool end) ReadUntilArrayEnd()
		{
			bool ok = Read();
			if (!ok)
			{
				return (false, false);
			}
			if (Key.Type == JsonValueType.JsonValueTypeArray && Value.Type == JsonValueType.JsonValueTypeEnd)
			{
				return (true, true);
			}
			return (true, false);
		}

		public bool Read()
		{
			if (Context.StackIndex == JsonContext.JsonStackSize)
			{
				Error = new Exception($"invalid JSON, current position at {Context.Cursor}");
				return false;
			}

			Key = JsonField.JsonFieldError;
			Value = JsonField.JsonFieldError;

			// The stack should only contain 'JsonValueTypeObject' or 'JsonValueTypeArray'
			JsonValueType state = Context.Stack[Context.StackIndex];
			bool wasReadOk = true;

			switch (state)
			{
				case JsonValueType.JsonValueTypeObject:
					var (okey, ovalue, oresult) = Context.ParseObjectBody();
					switch (oresult)
					{
						case JsonResult.JsonResultEmpty: // End of object
							Key = new JsonField { Begin = 0, Length = 0, Type = JsonValueType.JsonValueTypeObject };
							Value = new JsonField { Begin = 0, Length = 0, Type = JsonValueType.JsonValueTypeEnd };
							Context.StackIndex++;
							break;
						case JsonResult.JsonResultError:
							Error = new Exception($"error parsing object at {Context.Cursor}");
							wasReadOk = false;
							break;
						default:
							Key = okey;
							Value = ovalue;
							break;
					}
					break;

				case JsonValueType.JsonValueTypeArray:
					Key = new JsonField { Begin = 0, Length = 0, Type = JsonValueType.JsonValueTypeArray };
					var (avalue, aresult) = Context.ParseArrayBody();
					switch (aresult)
					{
						case JsonResult.JsonResultEmpty: // End of array
							Value = new JsonField { Begin = 0, Length = 0, Type = JsonValueType.JsonValueTypeEnd };
							Context.StackIndex++;
							break;
						case JsonResult.JsonResultError:
							Error = new Exception($"error parsing array at {Context.Cursor}");
							wasReadOk = false;
							break;
						default:
							Value = avalue;
							break;
					}
					break;

				default:
					Error = new Exception($"error reading at {Context.Cursor}");
					wasReadOk = false;
					break;
			}

			return wasReadOk;
		}

	}
}
