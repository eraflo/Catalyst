using System.Threading.Tasks;
using Eraflo.Catalyst.Command.Networking;

namespace Eraflo.Catalyst.Command
{
    public static class CommandExtensions
    {
        /// <summary>
        /// Executes the command through the global CommandManager.
        /// </summary>
        public static async Task Execute(this ICommand command)
        {
            await App.Get<CommandManager>().Execute(command);
        }

        /// <summary>
        /// Executes the command locally and broadcasts it to other networked clients.
        /// </summary>
        public static async Task ExecuteNetworked(this ICommand command)
        {
            // Execute locally
            await command.Execute();

            // Broadcast
            var network = App.Get<Eraflo.Catalyst.Networking.NetworkManager>();
            if (network != null && network.IsConnected)
            {
                var msg = new CommandNetworkMessage
                {
                    CommandType = command.GetType().AssemblyQualifiedName,
                    Payload = App.Get<Eraflo.Catalyst.Core.Save.SaveManager>()?.Serializer.Serialize(command)
                };
                network.Send(msg);
            }
        }
    }
}
