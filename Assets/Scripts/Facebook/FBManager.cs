﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Facebook.Unity;

public class FBManager : MonoBehaviour {

    private static FBManager _manager;

    public static FBManager manager
    {
        get
        {
            if (_manager == null)
            {
                GameObject fbm = new GameObject("FB Manager");
                fbm.AddComponent<FBManager>();
            }
            return _manager;
        }
    }

    public bool isloggedin { get; set; }
    public string profilename { get; set; }
    public Sprite profilepic { get; set; }
    public string AppLinkURL { get; set; }


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _manager = this;

    }
    public void InitFB()
    {
        if (!FB.IsInitialized)
        {
            FB.Init(SetInit, OnHideUnity);
        }
        else
        {
            isloggedin = FB.IsLoggedIn;
        }
    }

    void SetInit()
    {
        if (FB.IsLoggedIn)
        {
            Debug.Log("Logged in");
            getprofile();
        }
        else
        {
            Debug.Log("Not Logged in");
        }
        isloggedin = FB.IsLoggedIn;
    }

    void OnHideUnity(bool isShown)
    {
        if (isShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public void getprofile()
    {
        FB.API("/me?fields=first_name", HttpMethod.GET, ShowUsername);
        FB.API("/me/picture?type=square&height=128&width=128", HttpMethod.GET, ShowProfilePic);
        FB.GetAppLink(DealWithAppLink);
    }

    void ShowUsername(IResult result)
    {
        if (result.Error == null)
        {
            profilename = "" + result.ResultDictionary["first_name"];
        }
        else
        {
            Debug.Log(result.Error);
        }
    }

    void ShowProfilePic(IGraphResult result)
    {
        if (result.Texture != null)
        {
            profilepic = Sprite.Create(result.Texture, new Rect(0, 0, 128, 128), new Vector2());
        }
    }

    void DealWithAppLink(IAppLinkResult result)
    {
        if (!string.IsNullOrEmpty(result.Url))
        {
            AppLinkURL = ""+result.Url+"";

        }
        else
        {
            AppLinkURL = "https://www.youtube.com/watch?v=M94VA4cSTuc";
        }
    }

    public void Share()
    {
        FB.FeedShare(string.Empty,
            new Uri(AppLinkURL),
            "Attack on Tritan",
            "Caption",
            "Check out this game",
            new Uri("https://scontent-sit4-1.xx.fbcdn.net/v/t1.0-9/16729160_264888460614935_7431147129923963256_n.jpg?oh=8059d35a943a82ca11a40889cbe84de4&oe=59295E33"),
            string.Empty,
            ShareCallback
            );
    }

    void ShareCallback(IResult result)
    {
        if (result.Cancelled)
        {
            Debug.Log("Share Cancelled");
        }
        else if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Error on share!");
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            Debug.Log("Successfully shared");
        }
    }

    public void Invite()
    {
        FB.Mobile.AppInvite(
            new Uri(AppLinkURL),
            new Uri("https://scontent-sit4-1.xx.fbcdn.net/v/t1.0-9/16729160_264888460614935_7431147129923963256_n.jpg?oh=8059d35a943a82ca11a40889cbe84de4&oe=59295E33"),
            InviteCallback
            );
    }

    void InviteCallback(IResult result)
    {
        if (result.Cancelled)
        {
            Debug.Log("Invite Cancelled");
        }
        else if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Error on Invite!");
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            Debug.Log("Successfully Invited");
        }
    }

    public void ShareWithUsers()
    {
        FB.AppRequest("Try and beat my score",
            null,
            new List<object>() { "app_users" },
            null,
            null,
            null,
            null,
            ShareWithUsersCallback);
    }

    void ShareWithUsersCallback(IAppRequestResult result)
    {
        if (result.Cancelled)
        {
            Debug.Log("Challenge Cancelled");
        }
        else if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Error on Challenge!");
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            Debug.Log("Successfully Mentally Challenged");
        }
    }
}