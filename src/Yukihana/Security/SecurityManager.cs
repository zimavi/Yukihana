// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Collections.Concurrent;
using Yukihana.Debug;

namespace Yukihana.Security;

public sealed class SecurityManager
{
    private readonly ConcurrentDictionary<int, SecurityContext> _contexts = [];

    public SecurityContext? Get(Thread thread)
    {
        _contexts.TryGetValue(thread.ManagedThreadId, out SecurityContext? context);
        return context;
    }

    public void Set(Thread thread, SecurityContext context)
    {
        Logger.GlobalLogger.Trace($"Updating thread #{thread.ManagedThreadId} credentials");
        _contexts[thread.ManagedThreadId] = context;
    }

    public void Remove(Thread thread)
    {
        Logger.GlobalLogger.Trace($"Removing thread #{thread.ManagedThreadId} from contexts");
        _contexts.TryRemove(thread.ManagedThreadId, out _);
    }

    public void SetCurrent(SecurityContext context) => Set(Thread.CurrentThread, context);

    public SecurityContext Current
        => Get(Thread.CurrentThread)
           ?? throw new InvalidOperationException("Current thread does not have any context assigned to it");

    public Thread CreateThread(ThreadStart start, SecurityContext context)
    {
        ArgumentNullException.ThrowIfNull(start);

        Thread thread = new(() =>
        {
            _contexts[Thread.CurrentThread.ManagedThreadId] = context;

            try
            {
                start();
            }
            catch (Exception ex)
            {
                Logger.GlobalLogger.Error(ex.StackTrace ?? "<stacktrace null>");
            }
            finally
            {
                _contexts.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
            }
        });

        return thread;
    }
    public Thread CreateThread(ParameterizedThreadStart start, object? param, SecurityContext context)
    {
        ArgumentNullException.ThrowIfNull(start);

        Thread thread = new(() =>
        {
            _contexts[Thread.CurrentThread.ManagedThreadId] = context;

            try
            {
                start(param);
            }
            catch (Exception ex)
            {
                Logger.GlobalLogger.Error(ex.StackTrace ?? "<stacktrace null>");
            }
            finally
            {
                _contexts.TryRemove(Thread.CurrentThread.ManagedThreadId, out _);
            }
        });

        return thread;
    }

    public Thread CreateThread(ThreadStart start)
        => CreateThread(start, Current);

    public Thread CreateThread(ParameterizedThreadStart start, object? param)
        => CreateThread(start, param, Current);
}
