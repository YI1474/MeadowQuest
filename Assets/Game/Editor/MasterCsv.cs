using System;
using System.Collections.Generic;
using System.Text;

namespace MeadowQuest.Editor
{
    // Supports UTF-8 BOM, quoted commas, escaped quotes and multiline fields.
    public static class MasterCsv
    {
        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false, closed = false;
            text = text.TrimStart('\uFEFF');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c != '"')
                        field.Append(c);
                    else if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        closed = true;
                    }

                    continue;
                }

                if (c == ',' || c == '\r' || c == '\n')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    closed = false;
                    if (c != ',')
                    {
                        if (row.Count != 1 || row[0].Length != 0)
                            rows.Add(row.ToArray());
                        row.Clear();
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                            i++;
                    }
                }
                else if (closed)
                    throw new FormatException("引用符の後に区切り以外の文字があります。");
                else if (c == '"')
                {
                    if (field.Length != 0)
                        throw new FormatException("フィールド途中の引用符は使用できません。");
                    quoted = true;
                }
                else
                    field.Append(c);
            }

            if (quoted)
                throw new FormatException("CSVの引用符が閉じていません。");
            if (field.Length > 0 || row.Count > 0 || closed)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}
