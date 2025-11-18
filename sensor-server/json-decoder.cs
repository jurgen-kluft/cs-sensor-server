using System;
using System.Collections.Generic;
using System.Text;

namespace sensorserver
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
			return JsonValueTypeStringMap.GetValueOrDefault(valueType, "Unknown");
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
			return JsonResultStringMap.GetValueOrDefault(result, "Unknown");
		}
	}
	public class JsonField
	{
		public int Begin { get; set; }
		public int Length { get; set; }
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

	public partial class JsonContext
	{
		public string Json = string.Empty;
		public int Cursor = 0;
		public readonly bool IsEscapeString = true;
		public readonly JsonValueType[] Stack = new JsonValueType[JsonStackSize];
		public short StackIndex = JsonStackSize;
        private readonly StringBuilder EscapedStrings = new();
		private const short JsonStackSize = 256;

        public bool StackIsEmpty => StackIndex == JsonStackSize;
        public bool StackIsFull => StackIndex == 0;

        public void Reset()
        {
            Json = string.Empty;
            Cursor = 0;
            StackIndex = JsonStackSize;
            EscapedStrings.Clear();
        }

		public bool IsValidField(JsonField f) => f.Begin >= 0 && f.Length > 0 && (f.Begin + f.Length <= Json.Length);
	}
	public partial class JsonDecoder
	{
		public JsonContext Context { get; set; }
		public JsonField Key { get; set; }
		public JsonField Value { get; set; }
		public Exception Error { get; set; }

        private JsonDecoder()
		{
			Context = new JsonContext();
			Key = JsonField.sError;
			Value = JsonField.sError;
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
            Context.Reset();
			Key = JsonField.sError;
			Value = JsonField.sError;
			Error = null;
		}
	}

	public partial class JsonDecoder
	{
		public bool Decode(Dictionary<string, Action<JsonDecoder>> fields)
		{
			while (!ReadUntilObjectEnd())
			{
				var fname = DecodeField();
				if (fields.TryGetValue(fname.ToLowerInvariant(), out var fdecode))
				{
					fdecode(this);
				}
			}
			return true;
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
            while (!ReadUntilArrayEnd())
			{
				outArray.Add(DecodeString());
			}
			return outArray;
		}

		public void DecodeArray(Action<JsonDecoder> decodeElement)
		{
			while (!ReadUntilArrayEnd())
			{
				decodeElement(this);
			}
		}

		public List<List<string>> DecodeArrayArrayString()
		{
			var outArray = new List<List<string>>(4);
            while (!ReadUntilArrayEnd())
			{
				outArray.Add(DecodeStringArray());
			}
			return outArray;
		}

		public Dictionary<string, string> DecodeDictionaryStringString()
		{
			var outMap = new Dictionary<string, string>(4);
            while (!ReadUntilArrayEnd())
			{
				var key = DecodeField();
				var value = DecodeString();
				outMap[key] = value;
			}
			return outMap;
		}

        public byte[] DecodeByteArray()
		{
			List<byte> outBytes = new();
			while (!ReadUntilArrayEnd())
			{
				string str = DecodeString();
                byte b = str.ParseByte((byte)0);
				outBytes.Add(b);
			}
			return outBytes.ToArray();
		}

		public string FieldStr(JsonField f)
		{
			return Context.Json.Substring(f.Begin, f.Length);
		}

		public bool ParseBool(JsonField field)
		{
            if (!Context.IsValidField(field)) return false;

            try
            {
                return bool.Parse(FieldStr(field));
            }
            catch (Exception ex)
            {
                Error = ex;
            }
            return false;
		}

		public float ParseFloat32(JsonField field)
		{
            if (!Context.IsValidField(field)) return 0f;
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
			if (Context.IsValidField(field))
			{
				return FieldStr(field);
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
    		Error = new Exception($"invalid '{field}'");
			return false;
		}

		public bool ReadUntilObjectEnd()
		{
			bool ok = Read();
            return ok && (Key.Type == JsonValueType.JsonValueTypeObject && Value.Type == JsonValueType.JsonValueTypeEnd);
		}

		public bool ReadUntilArrayEnd()
		{
			bool ok = Read();
            return ok && (Key.Type == JsonValueType.JsonValueTypeArray && Value.Type == JsonValueType.JsonValueTypeEnd);
		}

		public bool Read()
		{
			if (Context.StackIsEmpty)
			{
				Error = new Exception($"invalid JSON, current position at {Context.Cursor}");
				return false;
			}

			Key = JsonField.sError;
			Value = JsonField.sError;

			// The stack should only contain 'JsonValueTypeObject' or 'JsonValueTypeArray'
			JsonValueType state = Context.Stack[Context.StackIndex];
			bool wasReadOk = true;
			switch (state)
			{
				case JsonValueType.JsonValueTypeObject:
					(JsonField okey, JsonField ovalue, JsonResult oresult) = Context.ParseObjectBody();
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
					(JsonField avalue, JsonResult aresult) = Context.ParseArrayBody();
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

	public partial class JsonContext
	{
        private JsonValueType DetermineValueType()
		{
			if (!SkipWhiteSpace())
			{
				return JsonValueType.JsonValueTypeError;
			}
			char current = Json[Cursor];
            return current switch
            {
                '{' => JsonValueType.JsonValueTypeObject,
                '[' => JsonValueType.JsonValueTypeArray,
                '"' => JsonValueType.JsonValueTypeString,
                '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' or '-' or '+' => JsonValueType.JsonValueTypeNumber,
                'f' or 'F' or 't' or 'T' => JsonValueType.JsonValueTypeBool,
                'n' => JsonValueType.JsonValueTypeNull,
                _ => JsonValueType.JsonValueTypeError
            };
        }

		public bool ParseBegin(string json)
		{
			Json = json;
			Cursor = 0;
			StackIndex = JsonStackSize;
			EscapedStrings.Clear();

			if (!SkipWhiteSpace())
				return false;

			char jsonByte = Json[Cursor];
			if (jsonByte == '}' || jsonByte == ',' || jsonByte == '"')
				return false;

			JsonValueType state = DetermineValueType();
			switch (state)
			{
				case JsonValueType.JsonValueTypeNumber:
				case JsonValueType.JsonValueTypeBool:
				case JsonValueType.JsonValueTypeString:
				case JsonValueType.JsonValueTypeNull:
					return true;

				case JsonValueType.JsonValueTypeArray:
					StackIndex--;
					Stack[StackIndex] = JsonValueType.JsonValueTypeObject; // Matches Go logic
					Cursor++; // skip '['
					return true;

				case JsonValueType.JsonValueTypeObject:
					StackIndex--;
					Stack[StackIndex] = JsonValueType.JsonValueTypeObject;
					Cursor++; // skip '{'
					return true;

                default:
					return false;
			}
		}

		public (JsonField outKey, JsonField outValue, JsonResult result) ParseObjectBody()
		{
			if (!SkipWhiteSpace())
			{
				return (JsonField.sError, JsonField.sError, JsonResult.JsonResultError);
			}

			if (Json[Cursor] == ',')
			{
				Cursor++;
				if (!SkipWhiteSpace())
				{
					return (JsonField.sError, JsonField.sError, JsonResult.JsonResultError);
				}
			}

			if (Json[Cursor] == '}')
			{
				Cursor++;
				return (JsonField.sError, JsonField.sError, JsonResult.JsonResultEmpty);
			}

			var result = JsonResult.JsonResultError;

			if (Json[Cursor] != '"')
			{
				// should be "
				var outKey = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeError);
				return (outKey, JsonField.sEmpty, result);
			}

			var keyField = GetString(); // get object key string

			if (SkipWhiteSpaceUntil(':') < 1)
			{
				keyField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeError);
				return (keyField, JsonField.sEmpty, result);
			}

			Cursor++; // skip ':'
			var state = DetermineValueType();
			result = (JsonResult)state;

			JsonField valueField;
			switch (state)
			{
				case JsonValueType.JsonValueTypeNumber:
					valueField = ParseNumber();
					break;
				case JsonValueType.JsonValueTypeBool:
					valueField = ParseBoolean();
					break;
				case JsonValueType.JsonValueTypeString:
					valueField = ParseString();
					break;
				case JsonValueType.JsonValueTypeNull:
					valueField = ParseNull();
					break;
				case JsonValueType.JsonValueTypeArray:
					if (StackIndex == 0)
					{
						keyField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeError);
						valueField = JsonField.sEmpty;
						result = JsonResult.JsonResultError;
					}
					else
					{
						StackIndex--;
						Stack[StackIndex] = JsonValueType.JsonValueTypeArray;
						valueField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeArray);
						Cursor++; // skip '['
					}
					break;
				case JsonValueType.JsonValueTypeObject:
					if (StackIndex == 0)
					{
						keyField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeError);
						result = JsonResult.JsonResultError;
						valueField = JsonField.sEmpty;
					}
					else
					{
						StackIndex--;
						Stack[StackIndex] = JsonValueType.JsonValueTypeObject;
						valueField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeObject);
						Cursor++; // skip '{'
					}
					break;
				default:
					keyField = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeError);
					valueField = JsonField.sEmpty;
					result = JsonResult.JsonResultError;
					break;
			}

			return (keyField, valueField, result);
		}

		public (JsonField outValue, JsonResult result) ParseArrayBody()
		{
			if (!SkipWhiteSpace())
			{
				return (JsonField.sError, JsonResult.JsonResultError);
			}

			if (Json[Cursor] == ',')
			{
				Cursor++;
				if (!SkipWhiteSpace())
				{
					return (JsonField.sError, JsonResult.JsonResultError);
				}
			}

			if (Json[Cursor] == ']')
			{
				Cursor++;
				return (JsonField.sEmpty, JsonResult.JsonResultEmpty);
			}

			JsonValueType state = DetermineValueType();
			JsonResult result = (JsonResult)state;
			JsonField outValue;

			switch (state)
			{
				case JsonValueType.JsonValueTypeNumber:
					outValue = ParseNumber();
					break;
				case JsonValueType.JsonValueTypeBool:
					outValue = ParseBoolean();
					break;
				case JsonValueType.JsonValueTypeString:
					outValue = ParseString();
					break;
				case JsonValueType.JsonValueTypeNull:
					outValue = ParseNull();
					break;
				case JsonValueType.JsonValueTypeArray:
					if (StackIndex == 0)
					{
						outValue = JsonField.sError;
						result = JsonResult.JsonResultError;
					}
					else
					{
						StackIndex--;
						Stack[StackIndex] = JsonValueType.JsonValueTypeArray;
						outValue = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeArray);
						Cursor++; // skip '['
					}
					break;
				case JsonValueType.JsonValueTypeObject:
					if (StackIndex == 0)
					{
						outValue = JsonField.sError;
						result = JsonResult.JsonResultError;
					}
					else
					{
						StackIndex--;
						Stack[StackIndex] = JsonValueType.JsonValueTypeObject;
						outValue = JsonField.NewJsonField(Cursor, 1, JsonValueType.JsonValueTypeObject);
						Cursor++; // skip '{'
					}
					break;
				default:
					outValue = JsonField.sEmpty;
					result = JsonResult.JsonResultError;
					break;
			}

			return (outValue, result);
		}

		public JsonField ParseString()
		{
			return GetString();
		}

		public JsonField ParseNumber()
		{
			var span = new JsonField { Begin = Cursor, Length = 0, Type = JsonValueType.JsonValueTypeNumber };
			while (Cursor < Json.Length)
			{
				char b = Json[Cursor];
				if ((b >= '0' && b <= '9') || b == '-' || b == '+' || b == '.' || b == 'e' || b == 'E')
				{
					Cursor++; // Move to the next character
					continue;
				}
				break;
			}

			span.Length = (Cursor - span.Begin);
			return span;
		}

		public JsonField ParseBoolean()
		{
			var span = new JsonField { Begin = Cursor, Length = 0, Type = JsonValueType.JsonValueTypeBool };
			if (!SkipWhiteSpace())
				return JsonField.sError;

			var (end, ok) = ScanUntilDelimiter();
			if (!ok)
				return JsonField.sError;

			span.Begin = Cursor;
			span.Length = (int)(end - Cursor);
			span.Type = JsonValueType.JsonValueTypeBool;
			Cursor += span.Length;
			return span;
		}

		public JsonField ParseNull()
		{
			var span = new JsonField { Begin = Cursor, Length = 0, Type = JsonValueType.JsonValueTypeNull };
			if (!SkipWhiteSpace())
				return JsonField.sError;

			var (end, ok) = ScanUntilDelimiter();
			if (!ok)
				return JsonField.sError;

			span.Begin = Cursor;
			span.Length = 0;
			span.Type = JsonValueType.JsonValueTypeError;

			int length = (end - Cursor);
			string token = Json.Substring(Cursor, length);

			if (length == 4 && token.Equals("null", StringComparison.OrdinalIgnoreCase))
			{
				span.Length = length;
			}
			else if (length == 3 && token.Equals("nil", StringComparison.OrdinalIgnoreCase))
			{
				span.Length = length;
			}

			if (span.Length > 0)
			{
				Cursor += span.Length;
				span.Type = JsonValueType.JsonValueTypeNull;
			}
			return span;
		}

		public (int end, bool ok) ScanUntilDelimiter()
		{
			int cursor = Cursor;
			while (cursor < Json.Length)
			{
				char ch = Json[cursor];
				switch (ch)
				{
					case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
                    case ',':
                    case ']':
                    case '}':
						return (cursor, true);
					default:
						cursor++;
						break;
				}
			}
			return (Cursor, false);
		}

		public bool SkipWhiteSpace()
		{
			while (Cursor < Json.Length)
			{
				char ch = Json[Cursor];
				switch (ch)
				{
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
						Cursor++;
						break;
					default:
						return true; // Next character is not whitespace
				}
			}
			return false; // End of JSON content
		}

		public int SkipWhiteSpaceUntil(char until)
		{
			while (Cursor < Json.Length)
			{
				char b = Json[Cursor];
				switch (b)
				{
                    case ' ':
                    case '\t':
                    case '\n':
                    case '\r':
						Cursor++;
						break;
					case var _ when b == until:
						return 1;
					default:
						return 0;
				}
			}
			return -1; // End of JSON content
		}

		public JsonField GetString()
		{
			Cursor += 1; // skip opening quote
			var start = Cursor;
			while (Cursor < Json.Length)
			{
				var current = Json[Cursor];
				switch (current)
				{
					case '"':
						Cursor++; // move past the closing quote
						return new JsonField { Begin = start, Length = ((Cursor - start) - 1), Type = JsonValueType.JsonValueTypeString };
					case '\\':
						Cursor += 2; // skip escaped character
						break;
					default:
						Cursor++;
						break;
				}
			}

			return JsonField.sError; // error if we reach here without finding a closing quote
		}

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
									uint codepoint;
									(codepoint, Cursor) = GetUnicodeCodePoint(Cursor);
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

		public (uint result, int index) GetUnicodeCodePoint(int cursor)
		{
			uint result = 0;
			var index = cursor;
			for (int i = 0; i < 4; i++)
			{
				result <<= 4;
				var ch = Json[index];
				if (ch >= '0' && ch <= '9')
					result |= (uint)(ch - '0');
				else if (ch >= 'A' && ch <= 'F')
					result |= (uint)(10 + (ch - 'A'));
				else if (ch >= 'a' && ch <= 'f')
					result |= (uint)(10 + (ch - 'a'));
				index++;
			}
			return (result, index);
		}
	}
}
