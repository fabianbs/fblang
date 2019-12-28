using CompilerInfrastructure.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompilerInfrastructure.Utils {
    using Structure.Types;

    public enum WarningLevel {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }
    public readonly struct ErrorMessage {
        /*public ErrorMessage(string message, int line, int col) {
            Message = message;
            Position = (line, col);
        }*/
        public ErrorMessage(string message, Position position) {
            Message = message;
            Position = position;
        }
        public ErrorMessage(string message) {
            Message = message;
            Position = null;
        }
        public string Message {
            get;
        }
        public Position? Position {
            get;
        }

        public override string ToString() {
            if (Position is null)
                return Message;
            else
                return Position.Value.ToString() + ": " + Message;
        }
    }
    public readonly struct WarningMessage {
        readonly ErrorMessage msg;
        readonly WarningLevel lvl;
        public WarningMessage(string message, Position position, WarningLevel level) {
            msg = new ErrorMessage(message, position);
            lvl = level;
        }
        public WarningMessage(string message, WarningLevel level) {
            msg = new ErrorMessage(message);
            lvl = level;
        }
        public WarningLevel Level => lvl;
        public string Message => msg.Message;
        public Position? Position => msg.Position;
        public override string ToString() {
            return $"Warning({lvl}): {msg}";
        }
    }
    public class ErrorBuffer : IEnumerable<ErrorMessage> {
        List<ErrorMessage> buf = new List<ErrorMessage>();
        List<WarningMessage> wbuf= new List<WarningMessage>();
        public void Add(ErrorMessage msg) {
            buf.Add(msg);
        }
        public void Add(WarningMessage msg) {
            wbuf.Add(msg);
        }
        public void Add(string msg) {
            buf.Add(new ErrorMessage(msg));
        }
        /*public void Add(string msg, int line, int col) {
            buf.Add(new ErrorMessage(msg, line, col));
        }*/
        public void Add(string msg, Position pos) {
            buf.Add(new ErrorMessage(msg, pos));
        }
        public void Add(string msg, Position pos, WarningLevel lvl) {
            wbuf.Add(new WarningMessage(msg, pos, lvl));
        }
        public T Add<T>(string msg, T ret) {
            Add(msg);
            return ret;
        }
        public T Add<T>(string msg, Position pos, T ret) {
            Add(msg, pos);
            return ret;
        }
        /*public T Add<T>(string msg, int line, int col, T ret) {
            Add(msg, line, col);
            return ret;
        }*/
        public void AddRange(IEnumerable<ErrorMessage> msgs) {
            if (msgs != null)
                buf.AddRange(msgs);
        }
        public void AddRange(ErrorBuffer buf) {
            if (buf != null) {
                this.buf.AddRange(buf.buf);
                this.wbuf.AddRange(buf.wbuf);
            }

        }
        public void Flush() {
            var errs = buf;
            var warns = wbuf;
            buf = new List<ErrorMessage>();
            wbuf = new List<WarningMessage>();
            foreach (var err in errs) {
                if (err.Position is null) {
                    err.Message.Report();
                }
                else {
                    err.Message.Report(err.Position.Value);
                }
            }
            foreach(var wrn in warns) {
                ErrorCollector.Warn(wrn);
            }
        }

        public IEnumerator<ErrorMessage> GetEnumerator() => ((IEnumerable<ErrorMessage>) buf).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<ErrorMessage>) buf).GetEnumerator();
    }
    public static class ErrorCollector {
        private static TextWriter errorOutput = Console.Error;

        public static TextWriter ErrorOutput {
            get {
                return errorOutput;
            }

            set {
                errorOutput = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public static bool HasErrors {
            get => NumErrors > 0;
        }
        public static int NumErrors {
            get; private set;
        }
        public static bool HasWarnings => NumWarnings > 0;
        public static int NumWarnings { get; private set; }

        public static void ResetErrors() {
            NumErrors = 0;
        }
        public static void ResetWarnings() {
            NumWarnings = 0;
        }

        public static void Report(this string message) {
            errorOutput.WriteLine(message);
            NumErrors++;
        }
        public static void Report(this string message, Position pos) {
            errorOutput.WriteLine($"{pos}: {message}");
            NumErrors++;
        }
        public static void Report(ErrorMessage message) {
            if (message.Position is null) {
                Report(message.Message);
            }
            else {
                Report(message.Message, message.Position.Value);
            }
        }
        public static void Warn(WarningMessage message) {
            errorOutput.WriteLine(message);
            NumWarnings++;
        }
        public static void Warn(this string message, WarningLevel lvl) {
            Warn(new WarningMessage(message, lvl));
        }
        public static void Warn(this string message, Position pos, WarningLevel lvl) {
            Warn(new WarningMessage(message, pos, lvl));
        }
        public static void Report(this string message, int line, int column) {
            errorOutput.WriteLine($"({line}, {column}): {message}");
            NumErrors++;
        }
        public static IType ReportTypeError(this string message) {
            message.Report();
            return Type.Error;
        }
        public static IType ReportTypeError(this string message, Position pos) {
            message.Report(pos);
            return Type.Error;
        }
        public static T Report<T>(this string message, T ret) {
            if (ret is IPositional pos)
                message.Report(pos.Position);
            else
                message.Report();
            return ret;
        }
        public static T Report<T>(this string message, Position pos, T ret) {
            message.Report(pos);
            return ret;
        }

        public static void Report(this ErrorBuffer buf, string msg) {
            if (buf != null)
                buf.Add(msg);
            else
                msg.Report();
        }
        /*public static void Report(this ErrorBuffer buf, string msg, int line, int col) {
            if (buf != null)
                buf.Add(msg, line, col);
            else
                msg.Report((line, col));
        }*/
        public static void Report(this ErrorBuffer buf, string msg, Position pos) {
            if (buf != null)
                buf.Add(msg, pos);
            else
                msg.Report(pos);
        }
        public static void ReportFrom(this ErrorBuffer buf, ErrorBuffer other) {
            if (other is null)
                return;
            if (buf is null) {
                foreach (var msg in other) {
                    Report(msg);
                }
            }
            else {
                buf.AddRange(other);
            }
        }
        public static T Report<T>(this ErrorBuffer buf, string msg, T ret) {
            buf.Report(msg);
            return ret;
        }
        public static T Report<T>(this ErrorBuffer buf, string msg, Position pos, T ret) {
            buf.Report(msg, pos);
            return ret;
        }
        public static IType ReportTypeError(this ErrorBuffer buf, string msg, Position? pos) {
            return pos.HasValue ? buf.Report(msg, pos.Value, Type.Error) : buf.Report(msg, Type.Error);
        }
        /*public static T Report<T>(this ErrorBuffer buf, string msg, int line, int col, T ret) {
            buf.Report(msg, line, col);
            return ret;
        }*/
    }
}
