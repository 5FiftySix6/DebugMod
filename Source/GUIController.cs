using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMod
{
    public class GUIController : MonoBehaviour
    {
        public Font trajanBold;
        public Font trajanNormal;
        public Font arial;
        public Dictionary<string, Texture2D> images = new Dictionary<string, Texture2D>();
        public Vector3 hazardLocation;
        public string respawnSceneWatch;

        public GameObject canvas;
        private static GUIController _instance;

        public void Awake()
        {
            hazardLocation = PlayerData.instance.hazardRespawnLocation;
            respawnSceneWatch = PlayerData.instance.respawnScene;
        }

        public void BuildMenus()
        {
            LoadResources();

            canvas = new GameObject();
            canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvas.AddComponent<GraphicRaycaster>();

            InfoPanel.BuildMenu(canvas);
            TopMenu.BuildMenu(canvas);
            EnemiesPanel.BuildMenu(canvas);
            Console.BuildMenu(canvas);
            KeyBindPanel.BuildMenu(canvas);

            DontDestroyOnLoad(canvas);
        }

        private void LoadResources()
        {
            foreach (Font f in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (f != null && f.name == "TrajanPro-Bold")
                {
                    trajanBold = f;
                }

                if (f != null && f.name == "TrajanPro-Regular")
                {
                    trajanNormal = f;
                }

                //Just in case for some reason the computer doesn't have arial
                if (f != null && f.name == "Perpetua")
                {
                    arial = f;
                }

                foreach (string font in Font.GetOSInstalledFontNames())
                {
                    if (font.ToLower().Contains("arial"))
                    {
                        arial = Font.CreateDynamicFontFromOSFont(font, 13);
                        break;
                    }
                }
            }

            if (trajanBold == null || trajanNormal == null || arial == null) DebugMod.instance.LogError("Could not find game fonts");

            string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            foreach (string res in resourceNames)
            {
                if (res.StartsWith("DebugMod.Images."))
                {
                    try
                    {
                        Stream imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
                        byte[] buffer = new byte[imageStream.Length];
                        imageStream.Read(buffer, 0, buffer.Length);

                        Texture2D tex = new Texture2D(1, 1);
                        tex.LoadImage(buffer.ToArray());

                        string[] split = res.Split('.');
                        string internalName = split[split.Length - 2];
                        images.Add(internalName, tex);

                        DebugMod.instance.Log("Loaded image: " + internalName);
                    }
                    catch (Exception e)
                    {
                        DebugMod.instance.LogError("Failed to load image: " + res + "\n" + e.ToString());
                    }
                }
            }
        }

        public void Update()
        {
            if (DebugMod.GM == null) return;
            
            InfoPanel.Update();
            TopMenu.Update();
            EnemiesPanel.Update();
            Console.Update();
            KeyBindPanel.Update();

            if (DebugMod.GetSceneName() == "Menu_Title") return;
            
            //Handle keybinds
            foreach (KeyValuePair<string, int> bind in DebugMod.settings.binds)
            {
                if (DebugMod.bindMethods.ContainsKey(bind.Key))
                {
                    if ((KeyCode)bind.Value == KeyCode.None)
                    {
                        foreach (KeyCode kc in Enum.GetValues(typeof(KeyCode)))
                        {
                            if (Input.GetKeyDown(kc))
                            {
                                if (KeyBindPanel.keyWarning != kc)
                                {
                                    foreach (KeyValuePair<string, int> kvp in DebugMod.settings.binds)
                                    {
                                        if (kvp.Value == (int)kc)
                                        {
                                            Console.AddLine(kc.ToString() + " already bound to " + kvp.Key + ", press again to confirm");
                                            KeyBindPanel.keyWarning = kc;
                                        }
                                    }

                                    if (KeyBindPanel.keyWarning == kc) break;
                                }

                                KeyBindPanel.keyWarning = KeyCode.None;

                                DebugMod.settings.binds[bind.Key] = (int)kc;
                                KeyBindPanel.UpdateHelpText();
                                break;
                            }
                        }
                    }
                    else if (Input.GetKeyDown((KeyCode)bind.Value))
                    {
                        try
                        {
                            ((MethodInfo)DebugMod.bindMethods[bind.Key].Second).Invoke(null, null);
                        }
                        catch (Exception e)
                        {
                            DebugMod.instance.LogError("Error running keybind method " + bind.Key + ":\n" + e.ToString());
                        }
                    }
                }
                else
                {
                    DebugMod.instance.LogWarn("Bind found without matching method, removing from binds: " + bind.Key);
                    DebugMod.settings.binds.Remove(bind.Key);
                }
            }

            if (DebugMod.infiniteSoul && PlayerData.instance.MPCharge < PlayerData.instance.maxMP && PlayerData.instance.health > 0 && !HeroController.instance.cState.dead && GameManager.instance.IsGameplayScene())
            {
                PlayerData.instance.MPCharge = PlayerData.instance.maxMP;
            }

            if (DebugMod.playerInvincible && PlayerData.instance != null)
            {
                PlayerData.instance.isInvincible = true;
            }

            if (DebugMod.noclip)
            {
                if (DebugMod.IH.inputActions.left.IsPressed)
                {
                    DebugMod.noclipPos = new Vector3(DebugMod.noclipPos.x - Time.deltaTime * 20f, DebugMod.noclipPos.y, DebugMod.noclipPos.z);
                }

                if (DebugMod.IH.inputActions.right.IsPressed)
                {
                    DebugMod.noclipPos = new Vector3(DebugMod.noclipPos.x + Time.deltaTime * 20f, DebugMod.noclipPos.y, DebugMod.noclipPos.z);
                }

                if (DebugMod.IH.inputActions.up.IsPressed)
                {
                    DebugMod.noclipPos = new Vector3(DebugMod.noclipPos.x, DebugMod.noclipPos.y + Time.deltaTime * 20f, DebugMod.noclipPos.z);
                }

                if (DebugMod.IH.inputActions.down.IsPressed)
                {
                    DebugMod.noclipPos = new Vector3(DebugMod.noclipPos.x, DebugMod.noclipPos.y - Time.deltaTime * 20f, DebugMod.noclipPos.z);
                }

                if (HeroController.instance.transitionState.ToString() == "WAITING_TO_TRANSITION")
                {
                    DebugMod.RefKnight.transform.position = DebugMod.noclipPos;
                }
                else
                {
                    DebugMod.noclipPos = DebugMod.RefKnight.transform.position;
                }
            }

            if (DebugMod.cameraFollow)
            {
                BindableFunctions.cameraGameplayScene.SetValue(DebugMod.RefCamera, false);
                DebugMod.RefCamera.SnapTo(DebugMod.RefKnight.transform.position.x, DebugMod.RefKnight.transform.position.y);
            }

            if (PlayerDeathWatcher.PlayerDied())
            {
                PlayerDeathWatcher.LogDeathDetails();
            }

            if (PlayerData.instance.hazardRespawnLocation != hazardLocation)
            {
                hazardLocation = PlayerData.instance.hazardRespawnLocation;
                Console.AddLine("Hazard Respawn location updated: " + hazardLocation.ToString());

                if (DebugMod.settings.EnemiesPanelVisible)
                {
                    EnemiesPanel.EnemyUpdate(200f);
                }
            }
            if (!string.IsNullOrEmpty(respawnSceneWatch) && respawnSceneWatch != PlayerData.instance.respawnScene)
            {
                respawnSceneWatch = PlayerData.instance.respawnScene;
                Console.AddLine(string.Concat(new string[]
                {
                    "Save Respawn updated, new scene: ",
                    PlayerData.instance.respawnScene.ToString(),
                    ", Map Zone: ",
                    GameManager.instance.GetCurrentMapZone(),
                    ", Respawn Marker: ",
                    PlayerData.instance.respawnMarkerName.ToString()
                }));
            }
        }

        public static GUIController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<GUIController>();
                    if (_instance == null)
                    {
                        DebugMod.instance.LogWarn("[DEBUG MOD] Couldn't find GUIController");

                        GameObject GUIObj = new GameObject();
                        _instance = GUIObj.AddComponent<GUIController>();
                        DontDestroyOnLoad(GUIObj);
                    }
                }
                return _instance;
            }
            set
            {
            }
        }
    }
}