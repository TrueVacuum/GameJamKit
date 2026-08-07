using System;
using System.Collections.Generic;
using System.Text;

namespace GameJamKit.Localization
{
    public static class LocalizationCsvParser
    {
        public static List<string[]> Parse(string csv)
        {
            List<string[]> rows = new List<string[]>();
            if (string.IsNullOrEmpty(csv))
            {
                return rows;
            }

            List<string> row = new List<string>();
            StringBuilder field = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char character = csv[i];

                if (insideQuotes)
                {
                    if (character == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            insideQuotes = false;
                        }
                    }
                    else if (character == '\r')
                    {
                        field.Append('\n');
                        if (i + 1 < csv.Length && csv[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == '"' && field.Length == 0)
                {
                    insideQuotes = true;
                }
                else if (character == ',')
                {
                    AddField(row, field);
                }
                else if (character == '\r' || character == '\n')
                {
                    AddField(row, field);
                    rows.Add(row.ToArray());
                    row.Clear();

                    if (character == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else
                {
                    field.Append(character);
                }
            }

            if (insideQuotes)
            {
                throw new FormatException("CSV contains an unterminated quoted field.");
            }

            if (field.Length > 0 || row.Count > 0)
            {
                AddField(row, field);
                rows.Add(row.ToArray());
            }

            return rows;
        }

        private static void AddField(List<string> row, StringBuilder field)
        {
            row.Add(field.ToString());
            field.Clear();
        }
    }
}
