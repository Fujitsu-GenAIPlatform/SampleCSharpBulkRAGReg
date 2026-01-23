using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SampleCSharpBulkRAGReg.Commons
{
    internal class LogHelper
    {
        private static　SynchronizationContext Context = SynchronizationContext.Current;
        private static StreamWriter LogWriter = null;

        /// <summary>
        /// ログファイル記録を開始する
        /// </summary>
        internal static void StartLog()
        {
            // ドキュメントフォルダ内のアプリ用サブフォルダにログを作成する
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var logDir = Path.Combine(documents, "SampleCSharpBulkRAGReg", "Logs");
            var LogFilePath = Path.Combine(logDir, $"{DateTime.Now:yyyyMMdd_HHmmss}_.log");

            try
            {
                Directory.CreateDirectory(logDir);
            }
            catch
            {
                // 作成に失敗した場合はフォールバックで実行ディレクトリを使用
                LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyyMMdd_HHmms}_.log");
            }

            //　ファイル開始
            try
            {
                // ファイルを追記モードで共有 (他プロセスからの読み取り/書き込み許可) にて開く
                var fs = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                LogWriter = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };
                WriteLog($"IsPublic = {App.MainVM.IsPublic}");
            }
            catch
            {
                // ファイルオープンに失敗した場合は LogWriter を null のままにしておく（ファイル出力を諦める）
                LogWriter = null;
            }
        }

        /// <summary>
        /// Log表示およびLogファイル記録   
        /// </summary>
        /// <param name="message"></param>
        internal static void WriteLog(string message, MessageBoxIcon mode = MessageBoxIcon.Information)
        {
            if (LogWriter is null)
            {
                DisplayLog("", true);
                StartLog();
            }
            if (!string.IsNullOrEmpty(message))
            {
                var modeMessage = string.Empty;
                switch (mode)
                {
                    case MessageBoxIcon.Error:
                        modeMessage = "[Error]";
                        break;
                    case MessageBoxIcon.Warning:
                        modeMessage = "[Warning]";
                        break;
                    case MessageBoxIcon.Information:
                        modeMessage = "[Info]";
                        break;
                    default:
                        break;
                }
                var messageWithTimestamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {modeMessage} {message}";
                DisplayLog(messageWithTimestamp + Environment.NewLine);
                try
                {
                    LogWriter?.WriteLine(messageWithTimestamp);
                    LogWriter?.Flush();
                }
                catch
                {
                    // 無視
                }

            }
        }

        private static void DisplayLog(string message, bool isInitialize = false)
        {
            try
            {
                Context.Post((o) =>
                {
                    if (isInitialize)
                    {
                        App.MainVM.LogText = string.Empty;
                    }
                    else
                    {
                        App.MainVM.LogText = App.MainVM.LogText + message;
                    }
                },
                null);
            }
            catch
            {
                // UI 更新が失敗しても続行
            }
        }

        /// <summary>
        /// ログファイル記録を終了する   
        /// </summary>
        internal static void EndLog()
        {
            if (LogWriter != null)
            {
                try
                {
                    WriteLog($"Process End");
                    LogWriter.Flush();
                    LogWriter.Dispose();
                }
                catch
                {
                    // 無視
                }
                finally
                {
                    LogWriter = null;
                }
            }
        }
    }
}
