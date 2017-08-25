using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows.Controls;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.Commands;
using Torch.Managers;
using VRage.Game;
using VRage.Game.Entity;

namespace Essentials
{
    [Plugin("Essentials", "1.5.1", "cbfdd6ab-4cda-4544-a201-f73efa3d46c0")]
    public class EssentialsPlugin : TorchPluginBase, IWpfPlugin
    {
        public EssentialsConfig Config => _config?.Data;

        private EssentialsControl _control;
        private Persistent<EssentialsConfig> _config;
        private static readonly Logger Log = LogManager.GetLogger("Essentials");
        private HashSet<ulong> _motdOnce = new HashSet<ulong>();

        /// <inheritdoc />
        public UserControl GetControl() => _control ?? (_control = new EssentialsControl(this));

        public void Save()
        {
            _config.Save();
        }

        /// <inheritdoc />
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _config = Persistent<EssentialsConfig>.Load(Path.Combine(StoragePath, "Essentials.cfg"));
            Torch.SessionLoaded += Torch_SessionLoaded;
        }

        private void Torch_SessionLoaded()
        {
            MyEntities.OnEntityAdd += MotdOnce;
            MyEntities.GetEntities().OfType<MyCharacter>().ForEach(MotdOnce);
        }

        private void ResetMotdOnce(MyCharacter character)
        {
            var identityId = character.ControllerInfo?.ControllingIdentityId ?? 0;
            if (Sync.Players.TryGetPlayerId(identityId, out MyPlayer.PlayerId playerId))
                _motdOnce.Remove(playerId.SteamId);
            character.CharacterDied -= ResetMotdOnce;
        }

        private void MotdOnce(MyEntity obj)
        {
            if (obj is MyCharacter character)
            {
                var identityId = character.ControllerInfo?.ControllingIdentityId ?? 0;
                if (!Sync.Players.TryGetPlayerId(identityId, out MyPlayer.PlayerId playerId))
                    return;
                if (string.IsNullOrEmpty(Config.Motd) || !_motdOnce.Add(playerId.SteamId))
                    return;
                Torch.Managers.GetManager<IChatManagerServer>().SendMessageAsOther("MOTD", Config.Motd, MyFontEnum.Blue, playerId.SteamId);
                character.CharacterDied += ResetMotdOnce;
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            Torch.SessionLoaded -= Torch_SessionLoaded;
            MyEntities.OnEntityAdd -= MotdOnce;
            MyEntities.GetEntities().OfType<MyCharacter>().ForEach(ResetMotdOnce);
            _config.Save();
        }
    }
}
