using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Accord.Web.Models;

public static class CommandCatalog
{
    public static IReadOnlyList<CommandGroup> Groups { get; } = Create();

    public static string Anchor(CommandGroup group) =>
        Regex.Replace(group.Name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

    private static List<CommandGroup> Create()
    {
        return
        [
            new CommandGroup("Profile & Participation",
                "View user profiles and server participation statistics.")
            {
                Commands =
                [
                    new Command("/profile", "Get your profile or another user's profile", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("userToLookup", "IUser", "User to look up (optional, defaults to yourself)", true, false)
                        ]
                    },
                    new Command("/leaderboard", "Get a leaderboard of XP and participation points", false, false, false),
                    new Command("/guildinfo", "Get guild information, recorded message activity, member counts, and coloured roles", false, false, false),
                    new Command("/calculate-xp", "Recalculate XP for all users", true, true, false),
                ]
            },
            new CommandGroup("Reminders",
                "Set, list, and manage personal reminders.")
            {
                Commands =
                [
                    new Command("/remind me", "Add a reminder for yourself", false, false, true)
                    {
                        Parameters =
                        [
                            new Param("timeSpan", "TimeSpan", "When to be reminded (e.g. 1h, 30m)", false, false),
                            new Param("message", "string", "Reminder message content", false, false)
                        ]
                    },
                    new Command("/remind list", "List your pending reminders", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("page", "int", "Page number (default: 1)", true, false)
                        ]
                    },
                    new Command("/remind delete", "Delete one of your reminders", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("reminderId", "int", "ID of the reminder to delete", false, false)
                        ]
                    },
                    new Command("/remind delete-all", "Delete all your reminders", false, true, false),
                    new Command("/remind list-user", "List another user's reminders", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("member", "IGuildMember", "User to look up", false, false),
                            new Param("page", "int", "Page number (default: 1)", true, false)
                        ]
                    },
                    new Command("/remind delete-user", "Delete a specific user's reminder", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("guildMember", "IGuildMember", "User whose reminder to delete", false, false),
                            new Param("reminderId", "int", "ID of the reminder to delete", false, false)
                        ]
                    },
                    new Command("/remind delete-user-all", "Delete all reminders for a user", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("guildMember", "IGuildMember", "User whose reminders to clear", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Tags",
                "Create, edit, search, and manage reusable content tags.")
            {
                Commands =
                [
                    new Command("/tag add", "Add a new tag", false, false, true),
                    new Command("/tag edit", "Edit an existing tag", false, false, true)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Name of the tag to edit", false, false)
                        ]
                    },
                    new Command("/tag delete", "Delete a tag", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Name of the tag to delete", false, false)
                        ]
                    },
                    new Command("/tag alias-add", "Add an alias to a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Tag name", false, false),
                            new Param("newAlias", "string", "Alias to add", false, false)
                        ]
                    },
                    new Command("/tag alias-delete", "Remove an alias from a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Alias to remove", false, false)
                        ]
                    },
                    new Command("/tag search", "Search tags by name or content", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("searchTerm", "string", "Search query", false, false)
                        ]
                    },
                    new Command("/tag info", "Get detailed information about a tag", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("name", "string", "Tag name", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("History & Moderation",
                "View and manage user history records, notes, and warnings.")
            {
                Commands =
                [
                    new Command("/note", "Add a private note to a user's history", false, true, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to note", false, false),
                            new Param("content", "string", "Note content (rest of message)", false, true)
                        ]
                    },
                    new Command("/warn", "Warn a user and optionally announce in channel", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to warn", false, false),
                            new Param("content", "string", "Warning reason", false, false),
                            new Param("announce", "bool", "Announce warning publicly (default: true)", true, false)
                        ]
                    },
                    new Command("/history get", "Get a single history record by ID", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("noteId", "int", "History record ID", false, false)
                        ]
                    },
                    new Command("/history delete", "Delete a history record", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("noteId", "int", "History record ID", false, false)
                        ]
                    },
                    new Command("/history list", "List all history records for a user", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User to look up", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Channel Flags",
                "Assign or remove flags that control bot behaviour per channel.")
            {
                Commands =
                [
                    new Command("/channel-flag add", "Add a flag to a channel", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Flag type (e.g. IgnoredFromXp, JoinLeaveLogs)", false, false),
                            new Param("channel", "IChannel", "Target channel (optional, defaults to current)", true, false)
                        ]
                    },
                    new Command("/channel-flag remove", "Remove a flag from a channel", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Flag type to remove", false, false),
                            new Param("channel", "IChannel", "Target channel (optional, defaults to current)", true, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Permissions",
                "Grant or revoke bot permissions for users and roles.")
            {
                Commands =
                [
                    new Command("/permission adduser", "Grant a permission to a user", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("member", "IGuildMember", "User to grant permission to", false, false),
                            new Param("type", "string", "Permission type (e.g. ManageFlags, ManageTags)", false, false)
                        ]
                    },
                    new Command("/permission addrole", "Grant a permission to a role", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("role", "IRole", "Role to grant permission to", false, false),
                            new Param("type", "string", "Permission type", false, false)
                        ]
                    },
                ]
            },
            new CommandGroup("Utilities",
                "C# evaluation, code compilation, URL shortening, and bot configuration.")
            {
                Commands =
                [
                    new Command("!eval", "Compile and run C# using the custom REPL, with NuGet package support. Also available as !exec and !e.", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("code", "C#", "Code to evaluate; code blocks are supported", false, true)
                        ]
                    },
                    new Command("/shorten", "Shorten a URL using the configured link shortener", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("url", "string", "URL to shorten (rest of message)", false, true)
                        ]
                    },
                    new Command("/godbolt", "Compile code via Godbolt compiler explorer", false, false, true),
                    new Command("/jumbo", "Show a full-size image of a custom or Unicode emoji", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("emoji", "string", "The custom or Unicode emoji to enlarge", false, false)
                        ]
                    },
                    new Command("/avatar", "Show your avatar or another user's avatar", false, false, false)
                    {
                        Parameters =
                        [
                            new Param("user", "IUser", "User whose avatar to show (defaults to yourself)", true, false),
                            new Param("size", "choice", "Image size: 16, 32, 64, 128 (default), 256, 512, 1024, 2048, or 4096", true, false)
                        ]
                    },
                    new Command("/configure", "Update a bot configuration option", true, true, false)
                    {
                        Parameters =
                        [
                            new Param("type", "string", "Configuration option name", false, false),
                            new Param("value", "string", "New value", false, false)
                        ]
                    },
                    new Command("/close", "Mark the current help forum thread as answered", false, true, false),
                    new Command("/github post-challenge", "Post a coding challenge from a GitHub repo", true, false, false)
                    {
                        Parameters =
                        [
                            new Param("readmeUrl", "string", "URL to the challenge README", false, false)
                        ]
                    },
                ]
            },
        ];
    }

    public sealed record CommandGroup(string Name, string? Description = null)
    {
        public List<Command> Commands { get; init; } = [];
    }

    public sealed record Command(string Usage, string Description, bool IsAdmin, bool IsEphemeral, bool IsModal)
    {
        public List<Param> Parameters { get; init; } = [];
    }

    public sealed record Param(string Name, string Type, string Description, bool IsOptional, bool IsGreedy);
}

