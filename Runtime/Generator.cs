using System;
using System.Text;

namespace FDB
{
    public interface IFuryGenerator<TDB>
    {
        void Execute(IndentStringBuilder sb, TDB db);
    }

    public sealed class IndentStringBuilder
    {
        readonly StringBuilder _sb = new StringBuilder();

        int _indent = 0;
        string _prefix = "";
        void UpdatePrefix() => _prefix = _indent == 0 ? string.Empty : new string('\t', _indent);

        public void BeginIndent()
        {
            _indent++;
            UpdatePrefix();
        }

        public void EndIndent()
        {
            _indent--;
            UpdatePrefix();
        }

        public IndentStringBuilder AppendLine(string text)
        {
            _sb.Append(_prefix);
            _sb.AppendLine(text);
            return this;
        }

        public override string ToString()
        {
            if (_indent != 0)
            {
                throw new Exception($"Indent is {_indent} excepted 0");
            }
            return _sb.ToString();
        }
    }
}