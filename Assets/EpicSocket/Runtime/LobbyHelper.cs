using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirage.Logging;
using UnityEngine;

namespace Mirage.Sockets.EpicSocket
{
    public class LobbyHelper
    {
        internal static readonly ILogger logger = LogFactory.GetLogger(typeof(LobbyHelper));
        private readonly ProductUserId _localUser;
        private readonly LobbyInterface _lobby;

        public LobbyHelper(ProductUserId localUser, LobbyInterface lobby)
        {
            _localUser = localUser;
            _lobby = lobby;
        }

        public UniTask StartLobby(int maxMembers, string bucketId = "Default")
        {
            var options = new CreateLobbyOptions
            {
                LocalUserId = _localUser,
                MaxLobbyMembers = (uint)maxMembers,
                PermissionLevel = LobbyPermissionLevel.Publicadvertised,
                PresenceEnabled = true,
                AllowInvites = true,
                DisableHostMigration = true,
                EnableRTCRoom = false,
                BucketId = bucketId,
            };
            return StartLobby(options);
        }
        public async UniTask StartLobby(CreateLobbyOptions options)
        {
            var awaiter = new AsyncWaiter<CreateLobbyCallbackInfo>();
            _lobby.CreateLobby(options, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Create Lobby", result.ResultCode);
            if (logger.LogEnabled()) logger.Log($"Lobby Created, ID:{result.LobbyId}");

            if (result.ResultCode != Result.Success)
                return;

            await ModifyLobby(result.LobbyId);
        }
        public async UniTask ModifyLobby(string lobbyId)
        {
            _lobby.UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = lobbyId, LocalUserId = _localUser }, out var modifyHandle);

            var data = CreateMapAttribute();
            modifyHandle.AddAttribute(new LobbyModificationAddAttributeOptions
            {
                Attribute = data,
                Visibility = LobbyAttributeVisibility.Public
            });

            var awaiter = new AsyncWaiter<UpdateLobbyCallbackInfo>();
            _lobby.UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modifyHandle }, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Modify Lobby", result.ResultCode);
            if (logger.LogEnabled()) logger.Log($"Lobby Modified, ID:{result.LobbyId}");
        }

        private static AttributeData CreateMapAttribute()
        {
            var data = new AttributeData();
            data.Key = "map";
            data.Value = new AttributeDataValue();
            data.Value.AsUtf8 = "test";
            return data;
        }

        public async UniTask<List<LobbyDetails>> GetAllLobbies(uint maxResults = 10)
        {
            logger.WarnResult("Create Search", _lobby.CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = maxResults, }, out var searchHandle));

            var awaiter = new AsyncWaiter<LobbySearchFindCallbackInfo>();

            var paramOptions = new LobbySearchSetParameterOptions
            {
                ComparisonOp = ComparisonOp.Equal,
                Parameter = CreateMapAttribute()
            };

            searchHandle.SetParameter(paramOptions);
            searchHandle.Find(new LobbySearchFindOptions { LocalUserId = _localUser, }, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Search Find", result.ResultCode);

            var getOption = new LobbySearchCopySearchResultByIndexOptions();
            var lobbyDetails = new List<LobbyDetails>();
            for (var i = 0; i < maxResults; i++)
            {
                getOption.LobbyIndex = (uint)i;
                var getResult = searchHandle.CopySearchResultByIndex(getOption, out var lobbyDetail);
                if (getResult == Result.Success)
                {
                    lobbyDetails.Add(lobbyDetail);
                }
                else
                {
                    logger.WarnResult("Search Get", result.ResultCode);
                }
            }

            searchHandle.Release();

            return lobbyDetails;
        }
    }
}

