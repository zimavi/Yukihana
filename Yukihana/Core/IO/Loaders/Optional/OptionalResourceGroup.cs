// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache 2.0 License. See LICENSE for details.

using Yukihana.Core.Debug;
using Yukihana.Core.Primitives;

namespace Yukihana.Core.IO.Loaders.Optional;

public sealed class OptionalResourceGroup<TState>
{
    private readonly string _name;
    private readonly Func<TState> _createState;
    private readonly Action<TState> _commit;
    private readonly List<OptionalResourceMember<TState>> _members = new();

    private readonly IResourceProvider _provider;

    public OptionalResourceGroup(
        string name,
        Func<TState> createState,
        Action<TState> commit,
        IResourceProvider provider)
    {
        _name = name;
        _createState = createState;
        _commit = commit;
        _provider = provider;
    }

    public OptionalResourceGroup<TState> Add(
        string relativePath,
        string description,
        Action<TState, byte[]> applyCallback)
    {
        _members.Add(new OptionalResourceMember<TState>(relativePath, description, applyCallback));
        return this;
    }

    public Option<TState> TryLoad()
    {
        Logger.Trace("OptionalResourceGroup -> TryLoad()");

        var groupTask = ShellPrint.CreateTask($"Loading asset group '{_name}'", "OptionalLoader").Progress(0).Display();

        Logger.Trace("ORG -> Creating state");

        TState state = _createState();

        List<(OptionalResourceMember<TState> Member, byte[] Data)> staged = new(_members.Count);

        Logger.Trace($"ORG -> To stage: {_members.Count}");

        foreach(var member in _members)
        {
            var memberTask = ShellPrint.CreateTask(
                $"Loading '{member.Description}' for group '{_name}'",
                "OptionalLoader")
                .Progress(0)
                .Display();
            
            Logger.Trace($"ORG -> Calling LoadProvider of type {_provider.GetType().Name}");

            Option<byte[]> result = _provider.TryLoad(member.Path);

            if (result.IsNone)
            {
                memberTask.Warn()
                    .WithText($"Optional group '{_name}' dropped: missing '{member.Description}'")
                    .Display();

                return Option<TState>.None();
            }

            Logger.Trace($"ORG -> Member '{member.Description}' loaded successfully");

            staged.Add((member, result.Value));

            memberTask.Ok().WithText($"Staged '{member.Description}' for group '{_name}'").Display();
            groupTask.Progress(GetProgress(staged.Count, _members.Count)).Display();
        }

        Logger.Trace("ORG -> All members have been staged. Applying them to state");

        groupTask.Work().WithText($"Applying asset group '{_name}'").Progress(0);

        int i = 0;
        foreach(var item in staged)
        {
            groupTask.Progress(GetProgress(i, staged.Count)).Display();

            Logger.Trace($"ORG -> Apply for '{item.Member.Description}'");
            
            item.Member.Apply(state, item.Data);
            i++;
        }

        Logger.Trace("ORG -> Commiting state");

        groupTask.Info().WithText($"Commiting asset group '{_name}'").Display();

        _commit(state);

        groupTask.Ok()
            .WithText($"Optional asset group '{_name}' loaded successfully.")
            .Display();
        
        return state;
    }

    private int GetProgress(int current, int max) => (int)Math.Floor((double)current / max * 100);
}