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

    public Option<TState> TryLoad() =>
        BootEnvironment.Stage == BootStage.EarlyKernel 
            ? TryLoadEarly() 
            : TryLoadCore();

    private Option<TState> TryLoadEarly()
    {
        var logger = new Logger("assetloader.optional");
        logger.Info("OptionalResourceGroup -> TryLoad()");

        logger.Info($"Loading asset group \"{_name}\"");

        TState state = _createState();

        List<(OptionalResourceMember<TState> Member, byte[] Data)> staged = new(_members.Count);

        logger.Info($"To stage: {_members.Count}");

        foreach(var member in _members)
        {
            logger.Info($"Loading \"'{member.Description}\" for group \"{_name}\"");

            Option<byte[]> result = _provider.TryLoad(member.Path);

            if (result.IsNone)
            {
                logger.Warn($"Group \"{_name}\" dropped: missing \"{member.Description}\"");

                return Option<TState>.None();
            }

            logger.Info($"Member \"{member.Description}\" loaded successfully");

            staged.Add((member, result.Value));

            logger.Info($"Staged \"{member.Description}\" for group \"{_name}\"");
        }

        logger.Info("All members have been staged. Applying them to state");

        int i = 0;
        foreach(var item in staged)
        {
            logger.Info($"Apply for \"{item.Member.Description}\"");
            
            item.Member.Apply(state, item.Data);
            i++;
        }

        logger.Info("Commiting state");

        _commit(state);

        logger.Info($"Optional asset group \"{_name}\" loaded successfully");
        
        return state;
    }

    private Option<TState> TryLoadCore()
    {
        UnitManager.Target("Optional Asset Group Load");

        var u = UnitManager.Start("Load", $"asset group \"{_name}\"");

        TState state = _createState();

        List<(OptionalResourceMember<TState> Member, byte[] Data)> staged = new(_members.Count);

        foreach(var member in _members)
        {
            
            var mu = UnitManager.Start("Load", $"\"{member.Description}\" for group \"{_name}\"");

            Option<byte[]> result = _provider.TryLoad(member.Path);

            if (result.IsNone)
            {
                mu.Fail();

                return Option<TState>.None();
            }

            mu.Ok();

            staged.Add((member, result.Value));
        }

        int i = 0;
        foreach(var item in staged)
        {
            UnitManager.Start("Apply", $"\"{item.Member.Description}\"");
            
            item.Member.Apply(state, item.Data);
            i++;
        }

        var uu = UnitManager.Start("Commit", $"assets to group \"{_name}\"");

        _commit(state);

        uu.Ok();
        u.Ok();
        
        return state;
    }
}