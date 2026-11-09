using System.Collections.Concurrent;

namespace FunkoApi.Back.Services.Email;

public class EmailQueueService {
    private readonly ConcurrentQueue<EmailMessage> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public void Enqueue(EmailMessage email) {
        _queue.Enqueue(email);
        _signal.Release();
    }

    public async Task<EmailMessage?> DequeueAsync(CancellationToken cancellationToken) {

        await _signal.WaitAsync(cancellationToken);
        return _queue.TryDequeue(out var email) ? email : null;
    }

    public int Count => _queue.Count;
}