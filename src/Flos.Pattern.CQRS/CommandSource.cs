using Flos.Identity;

namespace Flos.Pattern.CQRS;

/// <summary>Identifies the origin of a command.</summary>
/// <param name="Kind">The kind of command source.</param>
/// <param name="IssuerId">The entity that issued the command (for Player commands).</param>
public readonly record struct CommandSource(CommandSourceKind Kind, EntityId IssuerId = default)
{
    /// <summary>A command source representing the game system.</summary>
    public static CommandSource System => new(CommandSourceKind.System);

    /// <summary>Creates a player-issued command source.</summary>
    /// <param name="playerId">The entity ID of the player issuing the command.</param>
    /// <returns>A new <see cref="CommandSource"/> with <see cref="CommandSourceKind.Player"/> kind.</returns>
    public static CommandSource Player(EntityId playerId) => new(CommandSourceKind.Player, playerId);
}

/// <summary>The kind of command source.</summary>
public enum CommandSourceKind : byte
{
    /// <summary>Issued by the game system.</summary>
    System,

    /// <summary>Issued by a player entity.</summary>
    Player
}
