using System;
using UnityEditor;
using UnityEngine;

namespace EditorRemoting
{
    class GameViewScreenHelper
    {
        public static int ScaleFactor;

        static Vector2 CurrentScreenSize = Vector2.zero;
        static int Orientation = 0;

        public static void Rescale(int newScale)
        {
            if (newScale != ScaleFactor)
            {
                ScaleFactor = newScale;

                int wSet = (int)(CurrentScreenSize.x / ScaleFactor);
                int hSet = (int)(CurrentScreenSize.y / ScaleFactor);

                SetUpGameViewEx(wSet, hSet, Orientation, false);
            }
        }

        public static void SetUpGameView(int w, int h, int orientation, bool assignToCurrent = true)
        {
            Orientation = orientation;
            {
                if (CurrentScreenSize.x != w || CurrentScreenSize.y != h)
                {
                    int wSet = w;
                    int hSet = h;

                    SetUpGameViewEx(wSet, hSet, orientation);

                    CurrentScreenSize.x = wSet;
                    CurrentScreenSize.y = hSet;
                }
            }
        }

        public static void SetUpGameViewEx(int w, int h, int orientation, bool assignToCurrent = true)
        {
            Orientation = orientation;
            {
                if (CurrentScreenSize.x != w || CurrentScreenSize.y != h || assignToCurrent == false)
                {
                    Screen.orientation = (ScreenOrientation)orientation;

                    var name = string.Format("RemoteScreen {0} {1} {2}", (ScreenOrientation)orientation, w, h);
                    var index = -1;

                    var gameViewSizesInstance = UnityEditor.GameViewSizes.instance;
                    var cachedDisplayTextsResult = gameViewSizesInstance.GetGroup(gameViewSizesInstance.currentGroupType).GetDisplayTexts();

                    for (int i = 0; i < cachedDisplayTextsResult.Length; i++)
                    {
                        int ratioBracketIndex = cachedDisplayTextsResult[i].IndexOf('(');
                        if (ratioBracketIndex != -1)
                        {
                            // remove brackets at the end what are added automatically with ratio resolution info
                            cachedDisplayTextsResult[i] = cachedDisplayTextsResult[i].Substring(0, ratioBracketIndex - 1);
                        }

                        if (string.Equals(cachedDisplayTextsResult[i], name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                    {
                        gameViewSizesInstance.GetGroup(gameViewSizesInstance.currentGroupType).AddCustomSize(new GameViewSize(GameViewSizeType.FixedResolution, w, h, name));

                        index = cachedDisplayTextsResult.Length;
                    }

                    GameView.GetMainGameView().SizeSelectionCallback(index, null);
                }
            }
        }
    }
}