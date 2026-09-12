using System.Collections.Concurrent;

namespace DockManager.App.Integrations;

/// <summary>
/// Runs integration work (UI Automation, shell COM) on a dedicated background STA thread.
/// UIA and shell COM prefer STA, and keeping the work off the UI thread means the dock never
/// blocks while a browser decides to expose its accessibility tree. Work items run one at a time,
/// which also caps the cost of the most expensive integrations.
/// </summary>
public sealed class IntegrationWorker : IDisposable
{
    private readonly BlockingCollection<Action> _queue = new();
    private readonly Thread _thread;

    public IntegrationWorker()
    {
        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "DockManager.IntegrationWorker",
        };

        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
    }

    public Task<T> RunAsync<T>(Func<CancellationToken, T> work, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (cancellationToken.IsCancellationRequested)
        {
            completion.TrySetCanceled(cancellationToken);
            return completion.Task;
        }

        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));

        try
        {
            _queue.Add(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                try
                {
                    completion.TrySetResult(work(cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    completion.TrySetCanceled(cancellationToken);
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });
        }
        catch (InvalidOperationException)
        {
            // The worker has been disposed; report instead of throwing on the caller's thread.
            completion.TrySetException(new ObjectDisposedException(nameof(IntegrationWorker)));
        }

        return completion.Task;
    }

    public void Dispose()
    {
        _queue.CompleteAdding();
    }

    private void Run()
    {
        foreach (var action in _queue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                // A poisoned work item must not kill the worker thread.
            }
        }
    }
}
