using System;
using System.Collections.Generic;
using System.Linq;
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

        public UniTask<string> StartLobby(int maxMembers, string bucketId = "Default")
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

        public async UniTask<string> StartLobby(CreateLobbyOptions options)
        {
            var awaiter = new AsyncWaiter<CreateLobbyCallbackInfo>();
            _lobby.CreateLobby(options, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Create Lobby", result.ResultCode);
            if (logger.LogEnabled()) logger.Log($"Lobby Created, ID:{result.LobbyId}");

            if (result.ResultCode != Result.Success)
                throw new EpicLobbyException("Failed to create lobby", result);

            return result.LobbyId;
        }


        public async UniTask LeaveLobby(string lobbyId)
        {
            var options = new LeaveLobbyOptions
            {
                LobbyId = lobbyId,
                LocalUserId = _localUser
            };
            var awaiter = new AsyncWaiter<LeaveLobbyCallbackInfo>();
            _lobby.LeaveLobby(options, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Create Lobby", result.ResultCode);
        }

        public UniTask ModifyLobby(string lobbyId, AttributeData modifyData)
        {
            return ModifyLobby(lobbyId, new List<AttributeData>() { modifyData });
        }
        public async UniTask ModifyLobby(string lobbyId, IEnumerable<AttributeData> modifyData)
        {
            if (modifyData.Count() == 0)
                throw new ArgumentException("collectioon was empty", nameof(modifyData));

            _lobby.UpdateLobbyModification(new UpdateLobbyModificationOptions { LobbyId = lobbyId, LocalUserId = _localUser }, out var modifyHandle);

            foreach (var data in modifyData)
            {
                modifyHandle.AddAttribute(new LobbyModificationAddAttributeOptions
                {
                    Attribute = data,
                    Visibility = LobbyAttributeVisibility.Public
                });
            }

            var awaiter = new AsyncWaiter<UpdateLobbyCallbackInfo>();
            _lobby.UpdateLobby(new UpdateLobbyOptions { LobbyModificationHandle = modifyHandle }, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Modify Lobby", result.ResultCode);
            if (logger.LogEnabled()) logger.Log($"Lobby Modified, ID:{result.LobbyId}");
        }

        public static AttributeData CreateData(string key, string value)
        {
            var data = new AttributeData();
            data.Key = key;
            data.Value = new AttributeDataValue();
            data.Value.AsUtf8 = value;
            return data;
        }

        public UniTask<List<LobbyDetails>> GetAllLobbies(LobbySearchSetParameterOptions searchOption, uint maxResults = 10)
        {
            return GetAllLobbies(new List<LobbySearchSetParameterOptions>() { searchOption }, maxResults);
        }
        public async UniTask<List<LobbyDetails>> GetAllLobbies(IEnumerable<LobbySearchSetParameterOptions> searchOptions, uint maxResults = 10)
        {
            logger.WarnResult("Create Search", _lobby.CreateLobbySearch(new CreateLobbySearchOptions { MaxResults = maxResults, }, out var searchHandle));

            foreach (var option in searchOptions)
            {
                searchHandle.SetParameter(option);
            }

            var awaiter = new AsyncWaiter<LobbySearchFindCallbackInfo>();
            searchHandle.Find(new LobbySearchFindOptions { LocalUserId = _localUser, }, null, awaiter.Callback);
            var result = await awaiter.Wait();
            logger.WarnResult("Search Find", result.ResultCode);

            var getOption = new LobbySearchCopySearchResultByIndexOptions();
            var lobbyDetails = new List<LobbyDetails>();

            var resultCount = searchHandle.GetSearchResultCount(new LobbySearchGetSearchResultCountOptions());
            for (var i = 0; i < resultCount; i++)
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


    public class EpicLobbyException : EpicSocketException
    {
        public readonly CreateLobbyCallbackInfo Result;

        public EpicLobbyException(string message, CreateLobbyCallbackInfo result) : base(message)
        {
            Result = result;
        }
    }
}

