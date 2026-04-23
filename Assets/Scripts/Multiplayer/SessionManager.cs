using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityUtils;

public class SessionManager : Singleton<SessionManager>
{
    private ISession activeSession;
    public ISession ActiveSession
    {
        get => activeSession;
        private set
        {
            activeSession = value;
            Debug.Log("Active Session: " + (activeSession != null ? activeSession.Id : "None"));
        }
    }
    
    public string CurrentSessionCode => ActiveSession?.Code; 

    const string playerNamePropertyKey = "playerName";
    
    private NetworkManager networkManager;
    
    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("AuthenticationService Player ID: " + AuthenticationService.Instance.PlayerId);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async UniTask<Dictionary<string, PlayerProperty>> GetPlayerPropertiesAsync()
    {
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        
        return new Dictionary<string, PlayerProperty>{{playerNamePropertyKey, playerNameProperty}};
    }

    public async UniTask CreateSessionAsHost() 
    {
        try 
        {
            var playerProperties = await GetPlayerPropertiesAsync();

            var options = new SessionOptions()
            {
                MaxPlayers = 2,
                IsLocked = false,
                IsPrivate = false,
                PlayerProperties = playerProperties
            }.WithDistributedAuthorityNetwork();
            
            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log("Session ID: " + ActiveSession.Id);
            Debug.Log("Session Code: " + ActiveSession.Code);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
    }

    public async UniTask JoinSessionByCode(string code) 
    {
        try 
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code);
            Debug.Log("Successfully joined session!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join session: {e.Message}");
        }
    }

    //TODO: Implementar no lobby
    public async UniTask KickPlayer(string playerID)
    {
        if (!ActiveSession.IsHost) return;
        
        await ActiveSession.AsHost().RemovePlayerAsync(playerID);
    }

    //TODO: Implementar no lobby
    public async UniTask LeaveSession()
    {
        if (ActiveSession != null)
        {
            try
            {
                await ActiveSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                ActiveSession = null;
            }
        }
    }
}