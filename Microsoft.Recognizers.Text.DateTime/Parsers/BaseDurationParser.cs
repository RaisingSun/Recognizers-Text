﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using DateObject = System.DateTime;

namespace Microsoft.Recognizers.Text.DateTime
{
    public class BaseDurationParser : IDateTimeParser
    {
        public static readonly string ParserName = Constants.SYS_DATETIME_DURATION;

        private readonly IDurationParserConfiguration config;

        public BaseDurationParser(IDurationParserConfiguration configuration)
        {
            config = configuration;
        }

        public ParseResult Parse(ExtractResult result)
        {
            return this.Parse(result, DateObject.Now);
        }

        public DateTimeParseResult Parse(ExtractResult er, DateObject refTime)
        {
            var referenceTime = refTime;

            object value = null;
            if (er.Type.Equals(ParserName))
            {
                var innerResult = new DateTimeResolutionResult();

                innerResult = ParseNumerWithUnit(er.Text, referenceTime);
                if (!innerResult.Success)
                {
                    innerResult = ParseImplicitDuration(er.Text, referenceTime);
                }

                if (innerResult.Success)
                {
                    innerResult.FutureResolution = new Dictionary<string, string>
                    {
                        {TimeTypeConstants.DURATION, innerResult.FutureValue.ToString()}
                    };
                    innerResult.PastResolution = new Dictionary<string, string>
                    {
                        {TimeTypeConstants.DURATION, innerResult.PastValue.ToString()}
                    };
                    value = innerResult;
                }
            }

            var ret = new DateTimeParseResult
            {
                Text = er.Text,
                Start = er.Start,
                Length = er.Length,
                Type = er.Type,
                Data = er.Data,
                Value = value,
                TimexStr = value == null ? "" : ((DateTimeResolutionResult)value).Timex,
                ResolutionStr = ""
            };
            return ret;
        }

        // simple cases made by a number followed an unit
        private DateTimeResolutionResult ParseNumerWithUnit(string text, DateObject referenceTime)
        {
            var ret = new DateTimeResolutionResult();
            var numStr = string.Empty;
            var unitStr = string.Empty;

            // if there are spaces between nubmer and unit
            var ers = this.config.CardinalExtractor.Extract(text);
            if (ers.Count == 1)
            {
                var pr = this.config.NumberParser.Parse(ers[0]);
                var srcUnit = text.Substring(ers[0].Start + ers[0].Length ?? 0).Trim().ToLower();
                if (this.config.UnitMap.ContainsKey(srcUnit))
                {
                    numStr = pr.Value.ToString();
                    unitStr = this.config.UnitMap[srcUnit];

                    ret.Timex = "P" + (IsLessThanDay(unitStr) ? "T" : "") + numStr + unitStr[0];
                    ret.FutureValue = ret.PastValue = (double)pr.Value * this.config.UnitValueMap[srcUnit];
                    ret.Success = true;
                    return ret;
                }
            }

            // if there are NO spaces between number and unit
            var match = this.config.NumberCombinedWithUnit.Match(text);
            if (match.Success)
            {
                numStr = match.Groups["num"].Value;
                var srcUnit = match.Groups["unit"].Value.ToLower();
                if (this.config.UnitMap.ContainsKey(srcUnit))
                {
                    unitStr = this.config.UnitMap[srcUnit];

                    if ((double.Parse(numStr) > 1000) && (unitStr.Equals("Y") || unitStr.Equals("MON") || unitStr.Equals("W")))
                    {
                        return ret;
                    }

                    ret.Timex = "P" + (IsLessThanDay(unitStr) ? "T" : "") + numStr + unitStr[0];
                    ret.FutureValue = ret.PastValue = double.Parse(numStr) * this.config.UnitValueMap[srcUnit];
                    ret.Success = true;
                    return ret;
                }
            }

            match = this.config.AnUnitRegex.Match(text);
            if (match.Success)
            {
                numStr = match.Groups["half"].Success ? "0.5" : "1";
                var srcUnit = match.Groups["unit"].Value.ToLower();
                if (this.config.UnitMap.ContainsKey(srcUnit))
                {
                    unitStr = this.config.UnitMap[srcUnit];

                    ret.Timex = "P" + (IsLessThanDay(unitStr) ? "T" : "") + numStr + unitStr[0];
                    ret.FutureValue = ret.PastValue = double.Parse(numStr) * this.config.UnitValueMap[srcUnit];
                    ret.Success = true;
                    return ret;
                }
            }

            return ret;
        }

        // handle cases that don't contain nubmer
        private DateTimeResolutionResult ParseImplicitDuration(string text, DateObject referenceTime)
        {
            var ret = new DateTimeResolutionResult();
            var result = new DateTimeResolutionResult();
            // handle "all day" "all year"
            if (TryGetResultFromRegex(config.AllDateUnitRegex, text, "1", out result))
            {
                ret = result;
            }
            // handle "half day", "half year"
            if (TryGetResultFromRegex(config.HalfDateUnitRegex, text, "0.5", out result))
            {
                ret = result;
            }

            return ret;
        }

        private bool TryGetResultFromRegex(Regex regex, string text, string numStr, out DateTimeResolutionResult ret)
        {
            ret = new DateTimeResolutionResult();
            var match = regex.Match(text);
            if (match.Success)
            {
                var srcUnit = match.Groups["unit"].Value;
                if (this.config.UnitValueMap.ContainsKey(srcUnit))
                {
                    var unitStr = this.config.UnitMap[srcUnit];
                    ret.Timex = "P" + numStr + unitStr[0];
                    ret.FutureValue = ret.PastValue = double.Parse(numStr) * this.config.UnitValueMap[srcUnit];
                    ret.Success = true;
                }
            }
            return match.Success;
        }

        public static bool IsLessThanDay(string unit)
        {
            if (unit.Equals("S") || unit.Equals("M") || unit.Equals("H"))
            {
                return true;
            }
            return false;
        }
    }
}