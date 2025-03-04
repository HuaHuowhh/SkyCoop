﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SkyCoop;
using MelonLoader;

namespace GameServer
{
    class ServerHandle
    {
        public static Vector3 boiVector3 = new Vector3(0,0,0);
        public static Quaternion boiQuat = new Quaternion(0,0,0,0);
        public static Vector3 LastBlockVector3 = new Vector3(0, 0, 0);
        public static bool DarkShatalkerMode = false;
        public static string gametime = "12:0";
        public static bool LastLight = false;
        public static Vector3 LastFire = new Vector3(0, 0, 0);
        public static bool CanSend = false;
        public static bool LastReadyState = false;
        public static bool IamShatalker = false;
        public static bool LastWardIsActive = false;
        public static bool NeedReloadDWReadtState = false;
        public static float LastCountDown = 900f;
        public static int OverflowAnimalsOnConnectTimer = 0;

        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            OverflowAnimalsOnConnectTimer = 10;
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();
            string ModVersion = "";
            if (_packet.Length() > 1)
            {
                ModVersion = _packet.ReadString();
            }

            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
                return;
            }

            if(ModVersion != MyMod.BuildInfo.Version)
            {
                ServerSend.KICKMESSAGE(_fromClient, "Wrong mod version! Server using version "+ MyMod.BuildInfo.Version);
                MyMod.MultiplayerChatMessage DisconnectMessage = new MyMod.MultiplayerChatMessage();
                DisconnectMessage.m_Type = 0;
                DisconnectMessage.m_By = _username;
                DisconnectMessage.m_Message = _username + " can't join because has different mod version!";
                MyMod.SendMessageToChat(DisconnectMessage, true);
                MelonLogger.Msg("Client " + _fromClient + " has different version! Processing disconnect!");
                Server.clients[_fromClient].udp.Disconnect();
                return;
            }

            string SupporterID = _packet.ReadString();
            Supporters.SupporterBenefits ConfiguratedBenefits = _packet.ReadSupporterBenefits();
            MyMod.playersData[_fromClient].m_SupporterBenefits = Supporters.VerifyBenefitsWithConfig(SupporterID, ConfiguratedBenefits);
            Supporters.ApplyFlairsForModel(_fromClient, MyMod.playersData[_fromClient].m_SupporterBenefits.m_Flairs);
            CanSend = true;
            using (Packet __packet = new Packet((int)ServerPackets.GAMETIME))
            {
                ServerSend.GAMETIME(gametime);
            }

            MyMod.playersData[_fromClient].m_Name = _username;

            MelonLogger.Msg("Client "+ _fromClient+" with user name "+ _username+" connected!");
            MelonLogger.Msg("Sending init data to new client...");

            MelonLogger.Msg("[Init data] Client 0 -> Client "+_fromClient+ " Data from host player object");
            ServerSend.SERVERCFG(_fromClient);
            ServerSend.GEARPICKUPLIST(_fromClient);
            ServerSend.FURNBROKENLIST(_fromClient);
            ServerSend.ROPELIST(_fromClient);
            ServerSend.LOOTEDCONTAINERLIST(_fromClient);
            ServerSend.LOOTEDHARVESTABLEALL(_fromClient);
            ServerSend.ALLSHELTERS(_fromClient);

            int character = (int)GameManager.GetPlayerManagerComponent().m_VoicePersona;

            ServerSend.SELECTEDCHARACTER(0, character, false, _fromClient);

            ServerSend.XYZ(0, GameManager.GetPlayerTransform().position, false, _fromClient);
            ServerSend.XYZW(0, GameManager.GetPlayerTransform().rotation, false, _fromClient);
            ServerSend.LEVELID(0, MyMod.levelid, false, _fromClient);
            ServerSend.LEVELGUID(0, MyMod.level_guid, false, _fromClient);
            ServerSend.LIGHTSOURCE(0, MyMod.MyLightSource, false, _fromClient);
            ServerSend.LIGHTSOURCENAME(0, MyMod.MyLightSourceName, false, _fromClient);
            ServerSend.BENEFITINIT(0, Supporters.ConfiguratedBenefits, _fromClient);

            MyMod.PlayerEquipmentData Edata = new MyMod.PlayerEquipmentData();
            Edata.m_HasAxe = MyMod.MyHasAxe;
            Edata.m_HasMedkit = MyMod.MyHasMedkit;
            Edata.m_HasRevolver = MyMod.MyHasRevolver;
            Edata.m_HasRifle = MyMod.MyHasRifle;
            Edata.m_Arrows = MyMod.MyArrows;
            ServerSend.EQUIPMENT(0, Edata, false, _fromClient);
            MyMod.PlayerClothingData Cdata = new MyMod.PlayerClothingData();
            Cdata.m_Hat = MyMod.MyHat;
            Cdata.m_Top = MyMod.MyTop;
            Cdata.m_Bottom = MyMod.MyBottom;
            ServerSend.CLOTH(0, Cdata, false, _fromClient);

            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (Server.clients[i].IsBusy() == true)
                {
                    
                    if (MyMod.playersData[i] != null && i != _fromClient)
                    {
                        int PlayerIndex = i;
                        MelonLogger.Msg("[Init data] Client " + i + " -> Client " + _fromClient + " Data from playersData[" + PlayerIndex + "]");
                        int _FromId = i;
                        int _ForId = _fromClient;
                        MyMod.MultiPlayerClientData pD = MyMod.playersData[i];

                        ServerSend.XYZ(_FromId, pD.m_Position, false, _ForId);
                        ServerSend.XYZW(_FromId, pD.m_Rotation, false, _ForId);
                        ServerSend.LEVELID(_FromId, pD.m_Levelid, false, _ForId);
                        ServerSend.LEVELGUID(_FromId, pD.m_LevelGuid, true);
                        ServerSend.LIGHTSOURCE(_FromId, pD.m_PlayerEquipmentData.m_LightSourceOn, false, _ForId);
                        ServerSend.LIGHTSOURCENAME(_FromId, pD.m_PlayerEquipmentData.m_HoldingItem, false, _ForId);
                        ServerSend.EQUIPMENT(_FromId, pD.m_PlayerEquipmentData, false, _ForId);
                        ServerSend.CLOTH(_FromId, pD.m_PlayerClothingData, false, _ForId);
                        ServerSend.SELECTEDCHARACTER(_FromId, pD.m_Character, false, _ForId);
                        ServerSend.BENEFITINIT(_FromId, pD.m_SupporterBenefits, _fromClient);
                    }
                }
            }

            MyMod.SendSlotData(_fromClient);
            MyMod.NoHostResponceSeconds = 0;
            MyMod.SendRQEvent = false;
            MyMod.NeedTryReconnect = false;
            MyMod.TryingReconnect = false;

            MyMod.MultiplayerChatMessage joinMessage = new MyMod.MultiplayerChatMessage();
            joinMessage.m_Type = 0;
            joinMessage.m_By = _username;
            joinMessage.m_Message = _username + " join the server";

            MyMod.SendMessageToChat(joinMessage, false);

            ServerSend.CHAT(_fromClient, joinMessage, false);


            if (MyMod.CurrentCustomChalleng.m_Started)
            {
                ServerSend.CHALLENGEINIT(MyMod.CurrentChallengeRules.m_ID, MyMod.CurrentCustomChalleng.m_CurrentTask);
            }
        }
        public static void XYZ(int _fromClient, Packet _packet)
        {
            Vector3 maboi = _packet.ReadVector3();
            boiVector3 = new Vector3(maboi.x, maboi.y + 0.03f, maboi.z);

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Position = boiVector3;

                if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayer>() != null)
                {
                    MyMod.LongActionCancleCauseMoved(MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayer>());
                }
            }
            ServerSend.XYZ(_fromClient, boiVector3, false);
        }
        public static void XYZDW(int _fromClient, Packet _packet)
        {
            DarkShatalkerMode = true;
            Vector3 maboi;
            maboi = _packet.ReadVector3();
            boiVector3 = maboi;         
        }
        public static void XYZW(int _fromClient, Packet _packet)
        {
            Quaternion Rot = _packet.ReadQuaternion();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Rotation = Rot;
            }

            ServerSend.XYZW(_fromClient, Rot, false);
        }
        public static void BLOCK(int _fromClient, Packet _packet)
        {
            Vector3 V3 = _packet.ReadVector3();

            if (MyMod.playersData[_fromClient] != null)
            {
                if (MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.position = V3;
                }
            }
            ServerSend.BLOCK(_fromClient, V3, false);
        }
        public static void LEVELID(int _fromClient, Packet _packet)
        {
            int lel = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Levelid = lel;
                if(MyMod.playersData[_fromClient].m_Levelid != lel)
                {
                    MelonLogger.Msg("Player " + _fromClient + "  transition to level " + lel);
                }
                if(lel == 0)
                {
                    MyMod.AddLoadingClient(_fromClient);
                }
            }
            ServerSend.LEVELID(_fromClient, lel, false);
        }
        public static void LEVELGUID(int _fromClient, Packet _packet)
        {
            string lel = _packet.ReadString();
            
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_LevelGuid = lel;
                if (MyMod.playersData[_fromClient].m_LevelGuid != lel)
                {
                    MelonLogger.Msg("Player " + _fromClient + "  transition to level with GUID " + lel);
                }
            }
            ServerSend.LEVELGUID(_fromClient, lel, false);
        }
        public static void GOTITEM(int _fromClient, Packet _packet)
        {          
            MyMod.GearItemDataPacket got = _packet.ReadGearData();
            //MelonLoader.MelonLogger.Msg(ConsoleColor.Blue, "Client " + _fromClient + " gave item to" + got.m_SendedTo);
            //MelonLoader.MelonLogger.Msg(ConsoleColor.Blue, "Got gear with name [" + got.m_GearName + "] DATA: " + got.m_DataProxy);

            if (got.m_SendedTo == 0)
            {
                MyMod.GiveRecivedItem(got);
            }else{
                ServerSend.GOTITEM(got.m_SendedTo, got);
            }
        }
        public static void GOTITEMSLICE(int _fromClient, Packet _packet)
        {
            MyMod.SlicedJsonData got = _packet.ReadSlicedGear();
            if (got.m_SendTo == 0)
            {
                MyMod.AddSlicedJsonData(got);
            }else{
                ServerSend.GOTITEMSLICE(got.m_SendTo, got);
            }
        }
        public static void GAMETIME(int _fromClient, Packet _packet)
        {
            string got = _packet.ReadString();

            uConsole.RunCommand("set_time " + got);
        }
        public static void LIGHTSOURCENAME(int _fromClient, Packet _packet)
        {
            string Item = _packet.ReadString();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HoldingItem = Item;
            }

            ServerSend.LIGHTSOURCENAME(_fromClient, Item, false);
        }
        public static void LIGHTSOURCE(int _fromClient, Packet _packet)
        {
            bool On = _packet.ReadBool();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_LightSourceOn = On;
            }
            ServerSend.LIGHTSOURCE(_fromClient, On, false);
        }
        public static void ANIMSTATE(int _fromClient, Packet _packet)
        {
            string anim = _packet.ReadString();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_AnimState = anim;
            }

            ServerSend.ANIMSTATE(_fromClient, anim, false);
        }
        public static void REVIVE(int _fromClient, Packet _packet)
        {
            MyMod.SimRevive();
            using (Packet __packet = new Packet((int)ServerPackets.REVIVEDONE))
            {
                ServerSend.REVIVEDONE(1, true);
            }
        }
        public static void REVIVEDONE(int _fromClient, Packet _packet)
        {
            GameManager.GetInventoryComponent().RemoveGearFromInventory("GEAR_MedicalSupplies_hangar", 1);
        }
        public static void SLEEPHOURS(int _fromClient, Packet _packet)
        {
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_SleepHours = _packet.ReadInt();
            }
        }
        public static void DARKWALKERREADY(int _fromClient, Packet _packet)
        {
            LastReadyState = _packet.ReadBool();
            Console.WriteLine("Got new darkwalker ready state: " + LastReadyState);
        }
        public static void WARDISACTIVE(int _fromClient, Packet _packet)
        {
            LastWardIsActive = _packet.ReadBool();
        }
        public static void REQUESTDWREADYSTATE(int _fromClient, Packet _packet)
        {
            NeedReloadDWReadtState = true;
        }
        public static void DWCOUNTDOWN(int _fromClient, Packet _packet)
        {
            LastCountDown = _packet.ReadFloat();
            //Console.WriteLine("LastCountDown " + LastCountDown);
        }
        private static GearItem GetGearItemPrefab(string name) => Resources.Load(name).Cast<GameObject>().GetComponent<GearItem>();
        private static GameObject GetGearItemObject(string name) => Resources.Load(name).Cast<GameObject>();
        public static void SHOOTSYNC(int _fromClient, Packet _packet)
        {
            MyMod.ShootSync shoot = _packet.ReadShoot();
            MyMod.DoShootSync(shoot, _fromClient);

            ServerSend.SHOOTSYNC(_fromClient, shoot, false);
        }
        public static void PIMPSKILL(int _fromClient, Packet _packet)
        {
            int SkillTypeId = _packet.ReadInt();

            if (SkillTypeId == 1)
            {
                GameManager.GetSkillsManager().IncrementPointsAndNotify(SkillType.Rifle, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
                MelonLoader.MelonLogger.Msg("Got remote skill upgrade Rifle");
            }
            else if (SkillTypeId == 2)
            {
                GameManager.GetSkillsManager().IncrementPointsAndNotify(SkillType.Revolver, 1, SkillsManager.PointAssignmentMode.AssignOnlyInSandbox);
                MelonLoader.MelonLogger.Msg("Got remote skill upgrade Revolver");
            }
        }
        public static void HARVESTINGANIMAL(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_HarvestingAnimal = GUID;
            }
            ServerSend.HARVESTINGANIMAL(_fromClient, GUID, false);
        }
        public static void DONEHARVASTING(int _fromClient, Packet _packet)
        {
            MyMod.HarvestStats Harvey = _packet.ReadHarvest();
            MyMod.OnAnimalCorpseChanged(Harvey.m_Guid, Harvey.m_Meat, Harvey.m_Guts, Harvey.m_Hide);
        }
        public static void ANIMALTEST(int _fromClient, Packet _packet)
        {
            MyMod.AnimalCompactData dat = _packet.ReadAnimalCompactData();
            MyMod.AnimalAnimsSync anim = _packet.ReadAnimalAnim();
            int _for = _packet.ReadInt();

            if (_for == 0)
            {
                MyMod.DoAnimalSync(dat, anim);
            }else{
                ServerSend.ANIMALTEST(_fromClient, dat, anim, _for);
            }
        }
        public static void ANIMALSYNCTRIGG(int _fromClient, Packet _packet)
        {
            MyMod.AnimalTrigger got = _packet.ReadAnimalTrigger();
            MyMod.SetAnimalTriggers(got);
            ServerSend.ANIMALSYNCTRIGG(_fromClient, got, true);
        }
        public static void BULLETDAMAGE(int _fromClient, Packet _packet)
        {
            float damage = _packet.ReadFloat();
            int BodyPart = _packet.ReadInt();
            int _for = _packet.ReadInt();
            bool Melee = _packet.ReadBool();
            string MeleeWeapon = "";
            if (Melee)
            {
                MeleeWeapon = _packet.ReadString();
            }
            if (_for == 0)
            {
                MyMod.DamageByBullet(damage, _fromClient, BodyPart, Melee, MeleeWeapon);
            }else{
                ServerSend.BULLETDAMAGE(_for, damage, BodyPart, _fromClient, Melee, MeleeWeapon);
            }
        }
        public static void MULTISOUND(int _fromClient, Packet _packet)
        {
            string sound = _packet.ReadString();
            MyMod.PlayMultiplayer3dAduio(sound, _fromClient);
            ServerSend.MULTISOUND(_fromClient, sound, false);
        }
        public static void CONTAINEROPEN(int _fromClient, Packet _packet)
        {
            MyMod.ContainerOpenSync box = _packet.ReadContainer();
            MyMod.DoSyncContainer(box);

            ServerSend.CONTAINEROPEN(_fromClient, box, false);
        }
        public static void LUREPLACEMENT(int _fromClient, Packet _packet)
        {
            MyMod.WalkTracker sync = _packet.ReadWalkTracker();
            MyMod.LastLure = sync;
        }
        public static void LUREISACTIVE(int _fromClient, Packet _packet)
        {
            MyMod.LureIsActive = _packet.ReadBool();
        }
        public static void ALIGNANIMAL(int _fromClient, Packet _packet)
        {
            MyMod.AnimalAligner Alig = _packet.ReadAnimalAligner();
        }
        public static void ASKFORANIMALPROXY(int _fromClient, Packet _packet)
        {
            string _guid = _packet.ReadString();
            string Proxy = "";
            for (int i = 0; i < BaseAiManager.m_BaseAis.Count; i++)
            {
                if (BaseAiManager.m_BaseAis[i] != null && BaseAiManager.m_BaseAis[i].gameObject != null)
                {
                    GameObject animal = BaseAiManager.m_BaseAis[i].gameObject;
                    if (animal.GetComponent<ObjectGuid>() != null && animal.GetComponent<ObjectGuid>().Get() == _guid)
                    {
                        Proxy = BaseAiManager.m_BaseAis[i].Serialize();
                        break;
                    }
                }
            }
            using (Packet __packet = new Packet((int)ServerPackets.ALIGNANIMAL))
            {
                MyMod.AnimalAligner Alig = new MyMod.AnimalAligner();

                Alig.m_Guid = _guid;
                Alig.m_Proxy = Proxy;

                ServerSend.ALIGNANIMAL(1, Alig);
            }
        }
        public static void CARRYBODY(int _fromClient, Packet _packet)
        {
        }
        public static void BODYWARP(int _fromClient, Packet _packet)
        {

        }
        public static void ANIMALDELETE(int _fromClient, Packet _packet)
        {
            string AnimalGuid = _packet.ReadString();
            MyMod.DeleteAnimal(AnimalGuid);
            ServerSend.ANIMALDELETE(_fromClient, AnimalGuid);
        }
        public static void KEEPITALIVE(int _fromClient, Packet _packet)
        {
            MyMod.NoHostResponceSeconds = 0;

            int Been = Server.clients[_fromClient].TimeOutTime;

            Server.clients[_fromClient].TimeOutTime = 0;

            if(Been > 10)
            {
                MelonLogger.Msg(ConsoleColor.Yellow,"Last request from client "+_fromClient+ " took longer than expected");
                MelonLogger.Msg(ConsoleColor.Yellow, "Client[" + _fromClient + "] KeepItAlive took "+ Been+"s");
            }
        }
        public static void SYNCWEATHER(int _fromClient, Packet _packet)
        {

        }
        public static void EQUIPMENT(int _fromClient, Packet _packet)
        {
            MyMod.PlayerEquipmentData item = _packet.ReadEQ();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_Arrows = item.m_Arrows;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasAxe = item.m_HasAxe;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasMedkit = item.m_HasMedkit;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasRevolver = item.m_HasRevolver;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HasRifle = item.m_HasRifle;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_Flares = item.m_Flares;
                MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_BlueFlares = item.m_BlueFlares;
            }

            ServerSend.EQUIPMENT(_fromClient, item, false);
        }
        public static void CHAT(int _fromClient, Packet _packet)
        {
            MyMod.MultiplayerChatMessage message = _packet.ReadChat();
            MyMod.SendMessageToChat(message, false);
            ServerSend.CHAT(_fromClient, message, false);
        }
        public static void CHANGENAME(int _fromClient, Packet _packet)
        {
            string newName = _packet.ReadString();
            
            MyMod.MultiplayerChatMessage message = new MyMod.MultiplayerChatMessage();
            message.m_Type = 0;
            message.m_By = MyMod.playersData[_fromClient].m_Name;
            message.m_Message = MyMod.playersData[_fromClient].m_Name + " changed name to "+ newName;

            MyMod.playersData[_fromClient].m_Name = newName;

            MyMod.SendMessageToChat(message, true);
            ServerSend.CHANGENAME(_fromClient, newName, false);
        }

        public static void CLOTH(int _fromClient, Packet _packet)
        {
            MyMod.PlayerClothingData ClotchData = _packet.ReadClothingData();
            MyMod.playersData[_fromClient].m_PlayerClothingData = ClotchData;
            //MelonLoader.MelonLogger.Msg("[Clothing] Client " + _fromClient + " Hat " + MyMod.playersData[_fromClient].m_PlayerClothingData.m_Hat);
            //MelonLoader.MelonLogger.Msg("[Clothing] Client " + _fromClient + " Torso " + MyMod.playersData[_fromClient].m_PlayerClothingData.m_Top);
            //MelonLoader.MelonLogger.Msg("[Clothing] Client " + _fromClient + " Legs " + MyMod.playersData[_fromClient].m_PlayerClothingData.m_Bottom);
            //MelonLoader.MelonLogger.Msg("[Clothing] Client " + _fromClient + " Feets " + MyMod.playersData[_fromClient].m_PlayerClothingData.m_Boots);
            ServerSend.CLOTH(_fromClient, ClotchData, false);
        }

        public static void CONNECTSTEAM(int _fromClient, Packet _packet)
        {
            string sid = _packet.ReadString();
            int freeSlot = 0;

            bool ReConnection = false;

            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (Server.clients[i].udp != null && Server.clients[i].udp.sid == sid)
                {
                    ReConnection = true;
                    MelonLoader.MelonLogger.Msg("[SteamWorks.NET] Reconnecting " + sid + " as client " + i);
                    Server.clients[i].TimeOutTime = 0;
                    ServerSend.Welcome(freeSlot, Server.MaxPlayers);
                    freeSlot = i;
                    break;
                }
            }

            if(ReConnection == false)
            {
                for (int i = 1; i <= Server.MaxPlayers; i++)
                {
                    if (Server.clients[i].IsBusy() == false)
                    {
                        MelonLoader.MelonLogger.Msg("[SteamWorks.NET] Here an empty slot " + i + " for " + sid);
                        freeSlot = i;
                        Server.clients[i].udp.sid = sid;
                        ServerSend.Welcome(freeSlot, Server.MaxPlayers);
                        break;
                    }
                }
            }

            //MyMod.MultiplayerChatMessage joinMessage = new MyMod.MultiplayerChatMessage();
            //joinMessage.m_Type = 0;
            //joinMessage.m_By = sid;
            //joinMessage.m_Message = "STEAM USER "+sid + " trying to join server in slot "+freeSlot;

            //MyMod.SendMessageToChat(joinMessage, false);
        }
        public static void ASKSPAWNDATA(int _fromClient, Packet _packet)
        {
            int lvl = _packet.ReadInt();

            if(lvl == MyMod.levelid)
            {
                MyMod.SendSpawnData(false);
            }

            ServerSend.ASKSPAWNDATA(_fromClient, lvl, false);
        }
        public static void FURNBROKEN(int _fromClient, Packet _packet)
        {
            MyMod.BrokenFurnitureSync furn = _packet.ReadFurn();

            MyMod.OnFurnitureDestroyed(furn.m_Guid, furn.m_ParentGuid, furn.m_LevelID, furn.m_LevelGUID, false);

            ServerSend.FURNBROKEN(_fromClient, furn, false);
        }
        public static void FURNBREAKINGGUID(int _fromClient, Packet _packet)
        {
            MyMod.BrokenFurnitureSync furn = _packet.ReadFurn();

            if(MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BrakingObject = furn;

                if(MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
                {
                    MyMod.playersData[_fromClient].m_BrakingSounds = MyMod.GetBreakDownSound(furn);
                }
            }

            ServerSend.FURNBREAKINGGUID(_fromClient, furn, false);
        }
        public static void FURNBREAKINSTOP(int _fromClient, Packet _packet)
        {
            bool broken = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BrakingObject = new MyMod.BrokenFurnitureSync();
                MyMod.playersData[_fromClient].m_BrakingSounds = "";
            }

            ServerSend.FURNBREAKINSTOP(_fromClient, true, false);
        }
        public static void GEARPICKUP(int _fromClient, Packet _packet)
        {
            MyMod.PickedGearSync gear = _packet.ReadPickedGear();

            if (MyMod.playersData[_fromClient] != null && MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
            {
                if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>() != null)
                {
                    MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>().Pickup();
                }
            }

            MyMod.AddPickedGear(gear.m_Spawn, gear.m_LevelID, gear.m_LevelGUID, _fromClient, gear.m_MyInstanceID, false);

            ServerSend.GEARPICKUP(_fromClient, gear, false);
        }
        public static void ROPE(int _fromClient, Packet _packet)
        {
            MyMod.ClimbingRopeSync rope = _packet.ReadRope();

            MyMod.AddDeployedRopes(rope.m_Position, rope.m_Deployed, rope.m_Snapped, rope.m_LevelID, rope.m_LevelGUID, false);

            ServerSend.ROPE(_fromClient, rope, false);
        }
        public static void CONSUME(int _fromClient, Packet _packet)
        {
            bool IsDrink = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null && MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
            {
                if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>() != null)
                {
                    MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>().m_IsDrink = IsDrink;
                    MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>().Consumption();
                }
            }

            ServerSend.CONSUME(_fromClient, IsDrink, false);
        }
        public static void STOPCONSUME(int _fromClient, Packet _packet)
        {
            string LastAnim = _packet.ReadString();
            
            if (MyMod.playersData[_fromClient] != null)
            {
                if(MyMod.playersData[_fromClient].m_Levelid == MyMod.levelid && MyMod.playersData[_fromClient].m_LevelGuid == MyMod.level_guid)
                {
                    if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>() != null)
                    {
                        MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>().StopConsumption();
                    }
                }
                MyMod.playersData[_fromClient].m_AnimState = LastAnim;
            }
            ServerSend.STOPCONSUME(_fromClient, LastAnim, false);
        }
        public static void HEAVYBREATH(int _fromClient, Packet _packet)
        {
            bool IsHeavyBreath = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_HeavyBreath = IsHeavyBreath;
            }
            ServerSend.HEAVYBREATH(_fromClient, IsHeavyBreath, false);
        }
        public static void BLOODLOSTS(int _fromClient, Packet _packet)
        {
            int BloodCount = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_BloodLosts = BloodCount;
            }
            ServerSend.BLOODLOSTS(_fromClient, BloodCount, false);
        }
        public static void APPLYACTIONONPLAYER(int _fromClient, Packet _packet)
        {
            string ActionType = _packet.ReadString();
            int ForWho = _packet.ReadInt();

            if(ForWho == 0)
            {
                MyMod.OtherPlayerApplyActionOnMe(ActionType, _fromClient);
            }else{
                ServerSend.APPLYACTIONONPLAYER(_fromClient, ActionType, false, ForWho);
            }
        }
        public static void DONTMOVEWARNING(int _fromClient, Packet _packet)
        {
            bool ok = _packet.ReadBool();
            int ForWho = _packet.ReadInt();

            if(ForWho == 0)
            {
                if (MyMod.playersData[_fromClient] != null)
                {
                    HUDMessage.AddMessage("PLEASE DON'T MOVE, " + MyMod.playersData[_fromClient].m_Name + " IS TENDING YOU");
                    GameManager.GetVpFPSPlayer().Controller.Stop();
                }
            }else{
                ServerSend.DONTMOVEWARNING(_fromClient, true, false, ForWho);
            }
        }
        public static void INFECTIONSRISK(int _fromClient, Packet _packet)
        {
            bool InfRisk = _packet.ReadBool();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_NeedAntiseptic = InfRisk;
            }
            ServerSend.INFECTIONSRISK(_fromClient, InfRisk, false);
        }
        public static void CONTAINERINTERACT(int _fromClient, Packet _packet)
        {
            MyMod.ContainerOpenSync box = _packet.ReadContainer();

            if (MyMod.playersData[_fromClient] != null)
            {
                if(box.m_Guid == "NULL")
                {
                    MyMod.playersData[_fromClient].m_Container = null;
                }else{
                    MyMod.playersData[_fromClient].m_Container = box;
                }
            }

            if(box.m_Guid != "NULL")
            {
                MyMod.AddLootedContainer(box, true, _fromClient);
                if (box.m_LevelID == MyMod.levelid && box.m_LevelGUID == MyMod.level_guid)
                {
                    MyMod.ApplyLootedContainers();
                }
            }

            ServerSend.CONTAINERINTERACT(_fromClient, box, false);
        }
        public static void HARVESTPLANT(int _fromClient, Packet _packet)
        {
            MyMod.HarvestableSyncData harveData = _packet.ReadHarvestablePlant();

            if (MyMod.playersData[_fromClient] != null)
            {
                if (harveData.m_State == "Start")
                {
                    MyMod.playersData[_fromClient].m_Plant = harveData.m_Guid;
                }else{
                    MyMod.playersData[_fromClient].m_Plant = "";
                }
            }
            if(harveData.m_State == "Done")
            {
                MyMod.AddHarvastedPlant(harveData.m_Guid, _fromClient);
            }
            ServerSend.HARVESTPLANT(_fromClient, harveData, false);
        }

        public static void SELECTEDCHARACTER(int _fromClient, Packet _packet)
        {
            int character = _packet.ReadInt();

            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Character = character;
                if (character == 0)
                {
                    MyMod.playersData[_fromClient].m_Female = false;
                }
                if (character == 1)
                {
                    MyMod.playersData[_fromClient].m_Female = true;
                }
            }
            ServerSend.SELECTEDCHARACTER(_fromClient, character, false);
        }

        public static void ADDSHELTER(int _fromClient, Packet _packet)
        {
            MyMod.ShowShelterByOther shelter = _packet.ReadShelter();
            MyMod.ShelterCreated(shelter.m_Position, shelter.m_Rotation, shelter.m_LevelID, shelter.m_LevelGUID, false);
            ServerSend.ADDSHELTER(_fromClient, shelter,  false);
        }

        public static void REMOVESHELTER(int _fromClient, Packet _packet)
        {
            MyMod.ShowShelterByOther shelter = _packet.ReadShelter();
            MyMod.ShelterRemoved(shelter.m_Position, shelter.m_LevelID, shelter.m_LevelGUID, false);
            ServerSend.REMOVESHELTER(_fromClient, shelter, false);
        }
        public static void USESHELTER(int _fromClient, Packet _packet)
        {
            MyMod.ShowShelterByOther shelter = _packet.ReadShelter();
            if (MyMod.playersData[_fromClient] != null)
            {
                if (shelter.m_Position == new Vector3(0, 0, 0))
                {
                    MyMod.playersData[_fromClient].m_Shelter = null;
                }else{
                    MyMod.playersData[_fromClient].m_Shelter = shelter;
                }
            }
            ServerSend.USESHELTER(_fromClient, shelter, false);
        }
        public static void FIRE(int _fromClient, Packet _packet)
        {
            MyMod.FireSourcesSync FireSource = _packet.ReadFire();
            MyMod.MayAddFireSources(FireSource);
            ServerSend.FIRE(_fromClient, FireSource, false);
        }
        public static void FIREFUEL(int _fromClient, Packet _packet)
        {
            MyMod.FireSourcesSync FireSource = _packet.ReadFire();
            if (FireSource.m_LevelId == MyMod.levelid && FireSource.m_LevelGUID == MyMod.level_guid)
            {
                MyMod.AddOtherFuel(FireSource, FireSource.m_FuelName);
            }
            
            ServerSend.FIREFUEL(_fromClient, FireSource, false);
        }
        public static void CUSTOM(int _fromClient, Packet _packet)
        {
            API.CustomEventCallback(_packet, _fromClient);
        }
        public static void VOICECHAT(int _fromClient, Packet _packet)
        {
            int BytesWritten = _packet.ReadInt();
            int ReadLength = _packet.ReadInt();
            byte[] CompressedData = _packet.ReadBytes(ReadLength);
            float RecordTime = _packet.ReadFloat();
            int Sender = _packet.ReadInt();

            if(MyMod.playersData[_fromClient] != null)
            {
                string SourceLevel = MyMod.playersData[_fromClient].m_LevelGuid;
                float Radio = -66;

                if(_fromClient == Sender && MyMod.playersData[_fromClient].m_PlayerEquipmentData.m_HoldingItem == "GEAR_HandheldShortwave")
                {
                    Radio = MyMod.playersData[_fromClient].m_RadioFrequency;
                }

                if (SourceLevel == MyMod.level_guid)
                {
                    bool IsRadio = false;
                    if (_fromClient != Sender)
                    {
                        IsRadio = true;
                    }
                    MyMod.ProcessVoiceChatData(_fromClient, CompressedData, uint.Parse(BytesWritten.ToString()), RecordTime, IsRadio);
                }

                if(Radio == MyMod.RadioFrequency && !MyMod.DoingRecord)
                {
                    MyMod.ProcessRadioChatData(CompressedData, uint.Parse(BytesWritten.ToString()), RecordTime);
                    ServerSend.VOICECHAT(0, CompressedData, BytesWritten, RecordTime, MyMod.level_guid, -66, Sender);
                }

                ServerSend.VOICECHAT(_fromClient, CompressedData, BytesWritten, RecordTime, SourceLevel, Radio, Sender);
            }
        }
        public static void SLICEDBYTES(int _fromClient, Packet _packet)
        {
            MyMod.SlicedBytesData got = _packet.ReadSlicedBytes();
            MyMod.AddSlicedBytesData(got, _fromClient);

            if(got.m_SendTo != 0)
            {
                ServerSend.SLICEDBYTES(_fromClient, got, false, got.m_SendTo);
            }
        }
        public static void ANIMALDAMAGE(int _fromClient, Packet _packet)
        {
            string guid = _packet.ReadString();
            float damage = _packet.ReadFloat();
            MyMod.DoAnimalDamage(guid, damage);
            ServerSend.ANIMALDAMAGE(_fromClient, guid, damage);
        }
        public static void DROPITEM(int _fromClient, Packet _packet)
        {
            MyMod.DroppedGearItemDataPacket GearData = _packet.ReadDroppedGearData();
            MyMod.FakeDropItem(GearData);
            ServerSend.DROPITEM(_fromClient, GearData, true);
        }
        public static void GOTDROPSLICE(int _fromClient, Packet _packet)
        {
            MyMod.SlicedJsonData got = _packet.ReadSlicedGear();
            MyMod.AddSlicedJsonDataForDrop(got);
        }
        public static void REQUESTPICKUP(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            string Scene = _packet.ReadString();
            MelonLogger.Msg("Client "+ _fromClient+" trying to pickup gear with hash "+ Hash);
            MyMod.ClientTryPickupItem(Hash, _fromClient, Scene, false);
        }
        public static void REQUESTPLACE(int _fromClient, Packet _packet)
        {
            int Hash = _packet.ReadInt();
            string lvlKey = _packet.ReadString();
            MelonLogger.Msg("Client " + _fromClient + " trying to place gear with hash " + Hash);
            MyMod.ClientTryPickupItem(Hash, _fromClient, lvlKey, true);
        }

        public static void SendAllOpenables(int _fromClient, string scene)
        {
            Dictionary<string, bool> Opens = MPSaveManager.LoadOpenableThings(scene);
            if(Opens == null)
            {
                return;
            }

            foreach (var cur in Opens)
            {
                string currentKey = cur.Key;
                bool currentValue = cur.Value;
                ServerSend.USEOPENABLE(_fromClient, currentKey, currentValue);
            }
        }

        public static void SendAnimalCorpse(MyMod.AnimalKilled Animal, int forWho = -1)
        {
            if(forWho != -1)
            {
                ServerSend.ANIMALCORPSE(forWho, Animal);
            }else{
                ServerSend.ANIMALCORPSE(forWho, Animal, true);
            }
        }

        public static void RequestAnimalCorpses(int _fromClient, string LevelGUID)
        {
            List<string> ToRemove = new List<string>();

            foreach (var item in MyMod.AnimalsKilled)
            {
                int DespawnTime = item.Value.m_CreatedTime+14400;
                if (DespawnTime < MyMod.MinutesFromStartServer)
                {
                    ToRemove.Add(item.Key);
                }else{
                    if(item.Value.m_LevelGUID == LevelGUID)
                    {
                        SendAnimalCorpse(item.Value, _fromClient);
                    }
                }
            }
            foreach (var item in ToRemove)
            {
                MyMod.AnimalsKilled.Remove(item);
            }
        }

        public static void REQUESTDROPSFORSCENE(int _fromClient, Packet _packet)
        {
            int lvl = _packet.ReadInt();
            string Scene = _packet.ReadString();
            MelonLogger.Msg("Client "+ _fromClient+" request all drops for scene "+ Scene);
            if(MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Levelid = lvl;
                MyMod.playersData[_fromClient].m_LevelGuid = Scene;
            }
            SendAllOpenables(_fromClient, Scene);
            RequestAnimalCorpses(_fromClient, Scene);
            
            if(MyMod.CurrentCustomChalleng.m_Started && MyMod.CurrentChallengeRules.m_Name == "Lost in action")
            {
                ServerSend.CAIRNS(_fromClient);
            }

            foreach (MyMod.DeathContainerData create in MyMod.DeathCreates)
            {
                if(create.m_LevelKey == Scene)
                {
                    ServerSend.ADDDEATHCONTAINER(create, _fromClient);
                }
            }

            MyMod.ModifyDynamicGears(Scene);
            Dictionary<int, MyMod.DroppedGearItemDataPacket> Visuals = MPSaveManager.LoadDropVisual(Scene);
            Dictionary<int, MyMod.SlicedJsonDroppedGear> Drops = MPSaveManager.LoadDropData(Scene);


            if(Drops == null || Visuals == null)
            {
                MelonLogger.Msg("Requested scene has no drops " + Scene);
                ServerSend.LOADINGSCENEDROPSDONE(_fromClient, true);
            }else{
                int index = 0;
                foreach (var cur in Visuals)
                {
                    index++;
                    ServerSend.DROPITEM(0, cur.Value, false, _fromClient);
                }
                MelonLogger.Msg("Sending done, "+ index+" packets has been sent");                
                
                ServerSend.LOADINGSCENEDROPSDONE(_fromClient, true);
            }

            MyMod.RemoveLoadingClient(_fromClient);
        }
        public static void GOTCONTAINERSLICE(int _fromClient, Packet _packet)
        {
            MyMod.SlicedJsonData got = _packet.ReadSlicedGear();
            MyMod.AddSlicedJsonDataForContainer(got, _fromClient);
        }
        public static void REQUESTOPENCONTAINER(int _fromClient, Packet _packet)
        {
            string Scene = _packet.ReadString();
            string boxGUID = _packet.ReadString();
            MelonLogger.Msg("Client " + _fromClient + " request container data for " + boxGUID);
            string CompressedData = MPSaveManager.LoadContainer(Scene, boxGUID);

            if (CompressedData == "")
            {
                MelonLogger.Msg("Send to client this is empty");
                ServerSend.OPENEMPTYCONTAINER(_fromClient, true);
            }else{
                MelonLogger.Msg("Send to client data about container");
                MyMod.SendContainerData(CompressedData, Scene, boxGUID, _fromClient);
            }
        }
        public static void CHANGEAIM(int _fromClient, Packet _packet)
        {
            bool IsAiming = _packet.ReadBool();
            if (MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_Aiming = IsAiming;
            }
            ServerSend.CHANGEAIM(_fromClient, IsAiming, false);
        }
        public static void USEOPENABLE(int _fromClient, Packet _packet)
        {
            string Scene = _packet.ReadString();
            string _GUID = _packet.ReadString();
            bool state = _packet.ReadBool();
            MyMod.ChangeOpenableThingState(Scene, _GUID, state);
        }
        public static void TRYDIAGNISISPLAYER(int _fromClient, Packet _packet)
        {
            int ForWho = _packet.ReadInt();
            if (ForWho == 0)
            {
                MyMod.SendMyAffictions(_fromClient, GameManager.GetConditionComponent().m_CurrentHP);
            }else{
                ServerSend.TRYDIAGNISISPLAYER(ForWho, _fromClient);
            }
        }
        public static void CUREAFFLICTION(int _fromClient, Packet _packet)
        {
            MyMod.AffictionSync toCure = _packet.ReadAffiction();
            int ForWho = _packet.ReadInt();

            if(ForWho == 0)
            {
                MyMod.OtherPlayerCuredMyAffiction(toCure);
            }else{
                ServerSend.CUREAFFLICTION(ForWho, toCure);
            }
        }
        public static void ANIMALKILLED(int _fromClient, Packet _packet)
        {
            MyMod.AnimalKilled Data = _packet.ReadAnimalCorpse();
            MyMod.OnAnimalKilled(Data.m_PrefabName, Data.m_Position, Data.m_Rotation, Data.m_GUID, Data.m_LevelGUID, Data.m_RegionGUID, Data.m_Knocked);

            ServerSend.ANIMALCORPSE(0, Data, true);
        }
        public static void PICKUPRABBIT(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            int result = MyMod.PickUpRabbit(GUID);
            ServerSend.GOTRABBIT(_fromClient, result);
        }
        public static void HITRABBIT(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            int For = _packet.ReadInt();
            if(For != 0)
            {
                ServerSend.HITRABBIT(For, GUID);
            }else{
                MyMod.OnHitRabbit(GUID);
            }
            
        }
        public static void RELEASERABBIT(int _fromClient, Packet _packet)
        {
            string levelGUID = _packet.ReadString();
            MyMod.OnReleaseRabbit(_fromClient);
            ServerSend.RELEASERABBIT(_fromClient, levelGUID);
        }
        public static void SENDMYAFFLCTIONS(int _fromClient, Packet _packet)
        {
            int forWho = _packet.ReadInt();
            int Count = _packet.ReadInt();
            float hp = _packet.ReadFloat();
            List<MyMod.AffictionSync> Affs = new List<MyMod.AffictionSync>();

            for (int index = 0; index < Count; ++index)
            {
                MyMod.AffictionSync newElement = _packet.ReadAffiction();
                Affs.Add(newElement);
            }

            MelonLogger.Msg(ConsoleColor.Green, "Client " + _fromClient + " sent " + Count + " afflictions, for "+ forWho);

            if (forWho == 0)
            {
                MyMod.CheckOtherPlayer(Affs, _fromClient, hp);
            }else{
                ServerSend.SENDMYAFFLCTIONS(forWho, Affs, hp, _fromClient);
            }
        }
        public static void REQUESTANIMALCORPSE(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            MelonLogger.Msg("Client "+_fromClient+" requested animal corpse "+GUID);
            MyMod.AnimalKilled Animal;
            if (MyMod.AnimalsKilled.TryGetValue(GUID, out Animal))
            {
                float Meat = Animal.m_Meat;
                int Guts = Animal.m_Guts;
                int Hide = Animal.m_Hide;
                MelonLogger.Msg("Sending responce");
                ServerSend.REQUESTANIMALCORPSE(_fromClient, Meat, Guts, Hide);
            }else{
                ServerSend.REQUESTANIMALCORPSE(_fromClient, -1, 0, 0);
            }
        }
        public static void QUARTERANIMAL(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            MelonLogger.Msg("QUARTERANIMAL " + GUID);
            MyMod.OnAnimalQuarted(GUID);
            MyMod.SpawnQuartedMess(GUID);
            ServerSend.QUARTERANIMAL(_fromClient, GUID, false);
        }
        public static void ANIMALAUDIO(int _fromClient, Packet _packet)
        {
            bool InInt = _packet.ReadBool();
            string GUID = _packet.ReadString();
            if (InInt)
            {
                int soundID = _packet.ReadInt();
                string SenderLevelGUID = "";
                if (MyMod.playersData[_fromClient] != null)
                {
                    SenderLevelGUID = MyMod.playersData[_fromClient].m_LevelGuid;
                    if (MyMod.level_guid == MyMod.playersData[_fromClient].m_LevelGuid)
                    {
                        Pathes.Play3dAudioOnAnimal(GUID, soundID);
                    }
                }
                ServerSend.ANIMALAUDIO(_fromClient, soundID, GUID, SenderLevelGUID);
            }else{
                string soundID = _packet.ReadString();
                string SenderLevelGUID = "";
                if (MyMod.playersData[_fromClient] != null)
                {
                    SenderLevelGUID = MyMod.playersData[_fromClient].m_LevelGuid;
                    if (MyMod.level_guid == MyMod.playersData[_fromClient].m_LevelGuid)
                    {
                        Pathes.Play3dAudioOnAnimal(GUID, soundID);
                    }
                }
                ServerSend.ANIMALAUDIO(_fromClient, soundID, GUID, SenderLevelGUID);
            }
        }
        public static void CHANGEDFREQUENCY(int _fromClient, Packet _packet)
        {
            float FQ = _packet.ReadFloat();
            float FixedFloat = Mathf.Round(FQ * 10.0f) * 0.1f;

            if(MyMod.playersData[_fromClient] != null)
            {
                MyMod.playersData[_fromClient].m_RadioFrequency = FixedFloat;
            }
        }
        public static void MELEESTART(int _fromClient, Packet _packet)
        {
            if (MyMod.players[_fromClient] != null && MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>() != null)
            {
                MyMod.players[_fromClient].GetComponent<MyMod.MultiplayerPlayerAnimator>().MeleeAttack();
            }
            ServerSend.MELEESTART(_fromClient);
        }
        public static void TRYBORROWGEAR(int _fromClient, Packet _packet)
        {
            int For = _packet.ReadInt();
            string GearName = _packet.ReadString();
            if(For == 0)
            {
                MyMod.GiveBorrowedItem(GearName, For);
            }else{
                ServerSend.TRYBORROWGEAR(For, _fromClient, GearName);
            }
        }
        public static void CHALLENGETRIGGER(int _fromClient, Packet _packet)
        {
            string TRIGGER = _packet.ReadString();
            MyMod.ProcessCustomChallengeTrigger(TRIGGER);
            ServerSend.CHALLENGETRIGGER(TRIGGER);
        }
        public static void ADDDEATHCONTAINER(int _fromClient, Packet _packet)
        {
            MyMod.DeathContainerData Con = _packet.ReadDeathContainer();
            if (!MyMod.DeathCreates.Contains(Con))
            {
                MyMod.DeathCreates.Add(Con);
            }

            if (Con.m_LevelKey == MyMod.level_guid)
            {
                MyMod.MakeDeathCreate(Con);
            }

            ServerSend.ADDDEATHCONTAINER(Con, Con.m_LevelKey, _fromClient);
        }
        public static void DEATHCREATEEMPTYNOW(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            string Scene = _packet.ReadString();
            MyMod.RemoveDeathContainer(GUID, Scene);

            ServerSend.DEATHCREATEEMPTYNOW(GUID, Scene, _fromClient);
            MPSaveManager.RemoveContainer(Scene, GUID);
        }
        public static void SPAWNREGIONBANCHECK(int _fromClient, Packet _packet)
        {
            string GUID = _packet.ReadString();
            bool Result = MyMod.CheckSpawnRegionBanned(GUID);
            ServerSend.SPAWNREGIONBANCHECK(GUID, Result, _fromClient);
        }
    }
}
