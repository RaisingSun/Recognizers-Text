﻿using System.Text.RegularExpressions;

namespace Microsoft.Recognizers.Text.DateTime
{
    public interface IDateTimeExtractorConfiguration
    {
        Regex NowRegex { get; }
        Regex SuffixRegex { get; }
        Regex TimeOfTodayAfterRegex { get; }
        Regex SimpleTimeOfTodayAfterRegex { get; }
        Regex TimeOfTodayBeforeRegex { get; }
        Regex SimpleTimeOfTodayBeforeRegex { get; }
        Regex NightRegex { get; }
        Regex TheEndOfRegex { get; }
        Regex UnitRegex { get; }
        IExtractor DurationExtractor { get; }

        IExtractor DatePointExtractor { get; }
        IExtractor TimePointExtractor { get; }

        bool IsConnector(string text);

        bool GetAgoIndex(string text, out int index);
        bool GetLaterIndex(string text, out int index);
        bool GetInIndex(string text, out int index);
    }
}