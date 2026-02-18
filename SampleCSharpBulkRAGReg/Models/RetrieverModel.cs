using SampleCSharpBulkRAGReg.Commons;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SampleCSharpBulkRAGReg.Models
{
    internal class RetrieverModel : INotifyPropertyChanged
    {
        // IDトークン(有効期限があるのでMainVMから参照)
        private string IdToken { get { return App.MainVM.IdToken; } }

        // チャットルーム一覧（RAG割当の更新のために保持しておく）
        private List<Models.TDataChatRoom> ChatRooms = new List<Models.TDataChatRoom>();

        /// <summary>
        /// リトリーバー一覧を表すコレクション
        /// </summary>
        public ObservableCollection<Models.TDataRetriever> Retrievers { get; set; } = new ObservableCollection<Models.TDataRetriever>();

        /// <summary>
        /// 利用リトリーバー
        /// </summary>
        private Models.TDataRetriever _SelectedRetriever = null;
        public Models.TDataRetriever SelectedRetriever
        {
            get { return this._SelectedRetriever; }
            set
            {
                if (this._SelectedRetriever != value)
                {
                    this._SelectedRetriever = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 対象リトリーバ
        /// </summary>
        private APIData.TRetriever _Retriever = null;
        public APIData.TRetriever RetrieverData
        {
            get { return this._Retriever; }
            set
            {
                this._Retriever = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// リトリーバ名
        /// </summary>
        private string _RetrieverName = string.Empty;
        public string RetrieverName
        {
            get { return _RetrieverName; }
            set
            {
                _RetrieverName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンストラクター
        /// </summary>
        public RetrieverModel()
        {
        }

        /// <summary>
        /// リトリーバー一覧取得
        /// </summary>
        /// <returns></returns>
        internal async Task GetRetrieversAsync(bool isUseNone = false)
        {
            // API呼び出し
            var jsonString = await HttpHelper.GetRequestAsync("/api/v1/retrievers", this.IdToken);

            // 一覧取得
            this.Retrievers.Clear();

            // リトリーバーなしの選択を作成
            if (isUseNone)
            {
                this.Retrievers.Add(new Models.TDataRetriever()
                {
                    ID = string.Empty,
                    Name = "(none)"
                });
            }

            // JSONデシリアライズ
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetrievers));
                {
                    var result = ser.ReadObject(json) as APIData.TRetrievers;
                    foreach (var item in result.results)
                    {
                        var retriever = new Models.TDataRetriever()
                        {
                            ID = item.id,
                            Name = item.name,
                            IsOwner = item.owner == App.MainVM.UserId,
                            IsPublic = item.pub,
                            Created = item.created_at
                        };
                        this.Retrievers.Add(retriever);
                    }
                    json.Close();
                }
            }

            if (this.Retrievers.Count > 0)
            {
                this.SelectedRetriever = this.Retrievers[0];
            }
        }

        /// <summary>
        /// リトリーバー取得処理
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal async Task GetRetrieverAsync(string id)
        {
            var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                {
                    this.RetrieverData = ser.ReadObject(json) as APIData.TRetriever;
                    this.RetrieverName = this.RetrieverData.name.Trim();
                    json.Close();
                }
            }
        }

        /// <summary>
        /// フォルダ指定でのリトリーバー作成処理
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="isOverwrite">True:同名リトリーバを置換し、RAG割当済チャットルームも変更する</param>
        /// <returns></returns>
        /// <remarks>
        /// 指定したフォルダのサブフォルダをリトリーバー名として、サブフォルダ内のファイルをRAGデータとして登録する。
        /// </remarks>
        internal async Task CreateRetrieverFromFolder(string folderName, bool isOverwrite = false)
        {
            if (string.IsNullOrEmpty(folderName)) throw new ArgumentNullException(nameof(folderName));
            if (!Directory.Exists(folderName)) throw new DirectoryNotFoundException($"フォルダが見つかりません: {folderName}");

            // 指定フォルダ配下のサブフォルダを取得
            var subDirs = Directory.GetDirectories(folderName);

            // サブフォルダごとにリトリーバーを作成し、配下のファイルをRAGデータとして登録する
            await this.GetChatRoomsAsync();
            foreach (var subDir in subDirs)
            {
                var retrieverName = Path.GetFileName(subDir)?.Trim();
                var targetRetriever = await GetTargetRetriverAsync(retrieverName);

                if (string.IsNullOrEmpty(retrieverName))
                {
                    // サブフォルダ名が不正な場合はスキップ
                    LogHelper.WriteLog($"サブフォルダ名が取得できませんでした。パス: {subDir}");
                    continue;
                }

                try
                {
                    // サブフォルダ内のファイルを再帰的に取得（サブサブフォルダ内も対象）
                    var files = Directory.GetFiles(subDir, "*", SearchOption.AllDirectories)
                                         .Where(f => File.Exists(f))
                                         .Select(p => Path.GetFullPath(p))
                                         .ToList();

                    if (files.Count == 0)
                    {
                        LogHelper.WriteLog($"サブフォルダ '{subDir}' にファイルが見つかりません。スキップします。");
                        continue;
                    }
                    LogHelper.WriteLog($"リトリーバー '{retrieverName}' の作成を開始します。");
                    {
                        // リトリーバー作成とRAGデータ登録（内部で失敗時はクリーンアップする）
                        var id = await CreateRetrieverFromDataAsync(retrieverName, new List<string>(), files);

                        if (!string.IsNullOrEmpty(id))
                        {
                            LogHelper.WriteLog($"リトリーバー '{retrieverName}' を作成しました。ID={id}");
                            if (!string.IsNullOrEmpty(targetRetriever?.ID) && isOverwrite)
                            {
                                // 既存リトリーバーが存在し、置換指定ありの場合はチャットルームのRAG割当を更新する
                                try
                                {
                                    LogHelper.WriteLog($"リトリーバー '{retrieverName}' は既存のリトリーバーを置換します。");
                                    await this.ReplaceRetrieverInOwnedRoomsAsync(targetRetriever?.ID, id);

                                    // 既存リトリーバーを削除
                                    await this.DeleteRetrieverAsync(targetRetriever?.ID);
                                    LogHelper.WriteLog($"リトリーバー '{retrieverName}' の既存リトリーバーを削除しました。ID={targetRetriever?.ID}");
                                }
                                catch (Exception ex)
                                {
                                    // チャットルームの置換失敗はログに残して続行
                                    LogHelper.WriteLog($"リトリーバー '{retrieverName}' のチャットルームのRAG割当の置換に失敗しました: {ex.Message}");
                                    try
                                    {
                                        await this.DeleteRetrieverAsync(id);
                                        LogHelper.WriteLog($"リトリーバー '{retrieverName}' の追加をロールバックしました。ID={targetRetriever?.ID}");
                                    }
                                    catch { }
                                }
                            }
                        }
                        else
                        {
                            LogHelper.WriteLog($"リトリーバー '{retrieverName}' の作成に失敗しました。");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // サブフォルダ単位で例外をログに出して続行
                    LogHelper.WriteLog($"サブフォルダ '{subDir}' の処理に失敗しました: {ex.Message}");
                }
            }

            // 最後に一覧を更新（任意だがUI反映のため実行）
            try
            {
                await GetRetrieversAsync();
            }
            catch
            {
                // 一覧取得失敗はログに残して無視
                LogHelper.WriteLog("リトリーバー一覧の再取得に失敗しました。");
            }
        }

        // 共有リトリーバではないリトリーバーから名前で検索し、もっとも最近のものを返す
        private async Task<Models.TDataRetriever> GetTargetRetriverAsync(string name)
        {
            var target = this.Retrievers.Where((x) => !x.IsPublic && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).OrderByDescending((x) => x.Created).FirstOrDefault();
            return target;
        }

        /// <summary>
        /// リトリーバー作成処理
        /// </summary>
        /// <param name="retrieverName"></param>
        /// <param name="urls">URL指定</param>
        /// <param name="fileNames">ファイル/フォルダ指定</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal async Task<string> CreateRetrieverFromDataAsync(string retrieverName, List<string> urls, List<string> fileNames)
        {
            var item = await CreateRetrieverAsync(retrieverName);
            var id = item?.id;

            // アップロードファイルからリトリーバーを作成
            await EmbbedingFromDataAsync(item, urls, fileNames);
            return id;
        }

        // リトリーバー領域作成処理
        private async Task<APIData.TRetriever> CreateRetrieverAsync(string retrieverName)
        {
            var result = new APIData.TRetriever();
            var body = new APIData.TRetrieverRoom()
            {
                name = retrieverName,
                pub = App.MainVM.IsPublic,
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetrieverRoom));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/retrievers", this.IdToken, bodyJsonString);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                        {
                            result = ser.ReadObject(json) as APIData.TRetriever;
                            json.Close();
                        }
                    }
                }
            }
            return result;
        }

        // ベクター化処理
        private async Task<string> EmbbedingFromDataAsync(APIData.TRetriever retriever, List<string> urls, List<string> fileNames)
        {
            var id = retriever.id;
            var fileIds = new List<string>();

            try
            {
                // URLからのアップロード
                if (urls != null)
                {
                    var items = await UploadFromUrlsAsync(urls);
                    foreach (var item in items)
                    {
                        fileIds.Add(item);
                    }
                }

                // ファイルリストからのアップロード
                if (fileNames.Count > 0)
                {
                    var items = await UploadFromFilesAsync(fileNames);
                    foreach (var item in items)
                    {
                        fileIds.Add(item);
                    }
                }

                // ベクターデータ作成処理
                if (!string.IsNullOrEmpty(id))
                {
                    await EmbeddingAsync(id, fileIds, fileNames);
                }
            }
            catch (Exception ex)
            {
                // RAGデータをクリーンアップするために、ここでリトリーバー削除処理を追加する。  
                if (!string.IsNullOrEmpty(id))
                {
                    // ベクターデータ削除処理
                    await DeleteRetrieverAsync(id);
                }

                // アップロードファイル削除処理
                foreach (var fileId in fileIds)
                {
                    await DeleteFileAsync(fileId);
                }

                // 例外発生時のエラー処理は呼び出し元で行う。
                throw new Exception("ベクターデータ作成に失敗しました。", ex);
            }
            finally
            {
            }
            return id;
        }

        // URLからのアップロード
        private async Task<List<string>> UploadFromUrlsAsync(List<string> urls)
        {
            var id = string.Empty;
            var fileIds = new List<string>();

            // 登録するUrlを設定
            var prefix = $"{DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd")}_";
            var body = new TUploadDatas()
            {
                items = urls.Select(item => new TUploadData()
                {
                    name = $"{prefix}{item}",
                    url = item,
                    pub = App.MainVM.IsPublic,

                }).ToArray()
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TUploadDatas));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var requestBody = new MultipartFormDataContent();
                    var jsonContent = new StringContent(bodyJsonString, Encoding.UTF8, "application/json");
                    jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "body",
                    };
                    requestBody.Add(jsonContent);
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/files/from-url", this.IdToken, requestBody);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFiles));
                        {
                            var result = ser.ReadObject(json) as TFiles;

                            // LINQで書き換え: fileIdsを結果のid列から生成
                            fileIds = (result?.results ?? new TFile[0]).Select(item => item.id).ToList();
                            json.Close();
                        }
                    }
                }
            }
            return fileIds;
        }

        // ファイルからのアップロード
        private async Task<List<string>> UploadFromFilesAsync(List<string> fileNames)
        {
            var id = string.Empty;
            var fileIds = new List<string>();

            // 登録するUrlを設定
            var prefix = $"{DateTime.UtcNow.ToLocalTime().ToString("yyyyMMdd")}_";
            var body = new TUploadDatas()
            {
                items = fileNames.Select(item => new TUploadData()
                {
                    name = (item != null && item.StartsWith(prefix)) ? System.IO.Path.GetFileName(item) : $"{prefix}{System.IO.Path.GetFileName(item)}",
                    url = item,
                    pub = App.MainVM.IsPublic,
                }).ToArray()
            };

            // ここで body を JSON 文字列にシリアライズして変数に格納する
            using (var ms = new MemoryStream())
            {
                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TUploadDatas));
                {
                    serializer.WriteObject(ms, body);
                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                    var requestBody = new MultipartFormDataContent();
                    foreach (var path in body.items)
                    {
                        // 実ファイルのパスを渡している前提 (path がフルパス)
                        // 要件に合わせてファイルの読み込み方法を調整してください
                        byte[] fileBytes;
                        try
                        {
                            fileBytes = File.ReadAllBytes(path.url);
                        }
                        catch (Exception)
                        {
                            // ファイル読み込み失敗時はスキップ
                            continue;
                        }

                        // MIME タイプを調べる
                        var fileContent = new ByteArrayContent(fileBytes);
                        var mime = "application/octet-stream";
                        try
                        {
                            if (!string.IsNullOrEmpty(path.name))
                            {
                                var detected = GetMimeType(path.name);
                                if (!string.IsNullOrEmpty(detected))
                                {
                                    mime = detected;
                                }
                            }
                        }
                        catch
                        {
                            mime = "application/octet-stream";
                        }
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);

                        // Content-Disposition を設定（name="files"; filename="gand.html" 形式）
                        // Add(HttpContent, name, fileName) を使うと自動で Content-Disposition が正しく設定される
                        requestBody.Add(fileContent, "files", path.name);
                    }
                    var jsonContent = new StringContent(bodyJsonString, Encoding.UTF8, "application/json");
                    jsonContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "body",
                    };
                    requestBody.Add(jsonContent);
                    var jsonString = await HttpHelper.PostRequestAsync("/api/v1/files", this.IdToken, requestBody);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFiles));
                        {
                            var result = ser.ReadObject(json) as TFiles;

                            // LINQで書き換え: fileIdsを結果のid列から生成
                            fileIds = (result?.results ?? new TFile[0]).Select(item => item.id).ToList();
                            json.Close();
                        }
                    }
                }
            }
            return fileIds;
        }

        // ベクターデータ作成処理
        private async Task EmbeddingAsync(string id, List<string> fileIds, List<string> fileNames)
        {
            // Embeddingをシーケンシャルに実施するため、ファイルを１つづつ追加投入する
            for (var index = 0; index < fileIds.Count; index++)
            {
                var fileId = fileIds[index];

                // リトリーバー作成API呼び出し
                var body = new TFileIDs()
                {
                    file_ids = new string[] { fileId },
                };

                // ここで body を JSON 文字列にシリアライズして変数に格納する
                using (var ms = new MemoryStream())
                {
                    var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TFileIDs));
                    {
                        serializer.WriteObject(ms, body);
                        var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                        var jsonString = await HttpHelper.PostRequestAsync($"/api/v1/retrievers/{id}/process/embeddings", this.IdToken, bodyJsonString);
                    }
                }

                // 処理完了までポーリング
                var originalIds = new List<string>();
                while (true)
                {
                    var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TRetriever));
                        {
                            var result = ser.ReadObject(json) as APIData.TRetriever;
                            originalIds = result.origin_ids?.ToList();
                        }
                    }
                    var isExist = originalIds.Where((x) => x == fileId).FirstOrDefault() != null;
                    if (isExist) break;
                    await Task.Delay(1000);
                }
                LogHelper.WriteLog($"ファイル '{System.IO.Path.GetFileName(fileNames[index])}' のベクター化が成功しました。");
            }
        }

        // RAG削除処理
        internal async Task DeleteRetrieverAsync(string id)
        {
            await HttpHelper.DeleteRequestAsync($"/api/v1/retrievers/{id}", this.IdToken);
        }

        // ファイル削除処理
        private async Task DeleteFileAsync(string id)
        {
            await HttpHelper.DeleteRequestAsync($"/api/v1/files/{id}", this.IdToken);
        }

        // System.WebのMimeMappingは.NET Framework専用で、.NET Core/5+では利用できません。
        // 代替として、拡張子からMIMEタイプを判定する簡易メソッドを追加します。
        private string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
            switch (ext)
            {
                case ".txt": return "text/plain";
                case ".htm": return "text/html";
                case ".html": return "text/html";
                case ".csv": return "text/csv";
                case ".json": return "application/json";
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".png": return "image/png";
                case ".jpg": return "image/jpeg";
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                default: return "application/octet-stream";
            }
        }

        #region "チャットルーム関連"
        // 既存チャットルームのレトリーバー置換処理
        private async Task ReplaceRetrieverInOwnedRoomsAsync(string oldRetrieverID, string newRetrieverID)
        {
            foreach (var room in this.ChatRooms)
            {
                if (room.RetrieverIDs?.FirstOrDefault((x) => x == oldRetrieverID) != null)
                {
                    // 置換対象のレトリーバーが設定されている場合は、APIを呼び出してチャットルームのレトリーバーIDを置換する
                    var jsonString = await HttpHelper.GetRequestAsync($"/api/v1/chats/{room.ID}", this.IdToken);
                    using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
                    {
                        var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                        {
                            var roomData = ser.ReadObject(json) as APIData.TChat;

                            // LINQ による置換
                            if (roomData.retriever_ids != null && roomData.retriever_ids.Length > 0)
                            {
                                roomData.retriever_ids = roomData.retriever_ids
                                    .Select(id => string.Equals(id, oldRetrieverID, StringComparison.Ordinal) ? newRetrieverID : id)
                                    .ToArray();
                            }

                            json.Close();

                            // 更新APIコール 
                            using (var ms = new MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(APIData.TChat));
                                {
                                    serializer.WriteObject(ms, roomData);
                                    var bodyJsonString = Encoding.UTF8.GetString(ms.ToArray());
                                    jsonString = await HttpHelper.PutRequestAsync($"/api/v1/chats/{roomData.id}", this.IdToken, bodyJsonString);
                                }
                            }
                        }
                    }
                    LogHelper.WriteLog($"チャット '{System.IO.Path.GetFileName(room.Name)}' のリトリーバーを置換しました。");
                }
            }
        }

        // チャットルーム一覧を取得する
        private async Task GetChatRoomsAsync()
        {
            // API呼び出し
            var jsonString = await HttpHelper.GetRequestAsync("/api/v1/chats", this.IdToken);

            // 一覧取得
            this.ChatRooms.Clear();

            // JSONデシリアライズ
            using (var json = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonString)))
            {
                var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(List<APIData.TChat>));
                {
                    var result = ser.ReadObject(json) as List<APIData.TChat>;
                    foreach (var chat in result)
                    {
                        var chatRoom = new Models.TDataChatRoom()
                        {
                            ID = chat.id,
                            Name = chat.name,
                            ChatTemplateId = chat.chat_template_id,
                            RetrieverIDs = chat.retriever_ids,
                        };
                        this.ChatRooms.Add(chatRoom);
                    }
                    json.Close();
                }
            }
        }
        #endregion

        #region "JSON関連"
        [DataContract]
        public class TUploadDatas
        {
            [DataMember(Name = "params")]
            public TUploadData[] items { get; set; }
        }

        [DataContract]
        public class TUploadData
        {
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string url { get; set; }
            [DataMember(Name = "public")]
            public bool pub { get; set; }
        }

        [DataContract]
        public class TFiles
        {
            [DataMember]
            public TFile[] results { get; set; }
        }

        [DataContract]
        public class TFile
        {
            [DataMember]
            public string id { get; set; }
            [DataMember]
            public string name { get; set; }
            [DataMember]
            public string owner { get; set; }
            [DataMember(Name = "public")]
            public string pub { get; set; }
            [DataMember]
            public string[] origin_ids { get; set; }
            [DataMember]
            public bool is_deleted { get; set; }
            [DataMember]
            public long created_at { get; set; }
            [DataMember]
            public long? updated_at { get; set; }
            [DataMember]
            public long? deleted_at { get; set; }
            [DataMember]
            public string key { get; set; }
            [DataMember]
            public string content_type { get; set; }
            [DataMember]
            public string file_extension { get; set; }
            [DataMember]
            public string source_name { get; set; }
            [DataMember]
            public string storage_path { get; set; }
            [DataMember]
            public string target_storage { get; set; }
            [DataMember]
            public Metadata metadata { get; set; }
            [DataMember]
            public string task_id { get; set; }
        }

        [DataContract]
        public class Metadata
        {
        }

        [DataContract]
        public class TFileIDs
        {
            [DataMember]
            public string[] file_ids { get; set; }
        }
        #endregion

        // プロパティが変更されたときに通知するイベント
        public event PropertyChangedEventHandler PropertyChanged;

        // プロパティ変更通知を発行するメソッド
        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}