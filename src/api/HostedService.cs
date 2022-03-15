namespace api;

public class HostedServiceFaceDetect : IHostedService
{
    private Task _task;
    private CancellationTokenSource _cts;

    public HostedServiceFaceDetect()
    {
        
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _task = Task.Factory.StartNew(() => Start(_cts.Token), _cts.Token);
        return _task;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
    
    

    private async Task Start(CancellationToken ct)
    {
        // TODO: below part can probably be moved into StartAsync and later disposed in StopAsync
        using var watcher = new FileSystemWatcher("./processFolder");
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
        watcher.Created += WatcherOnCreated;
        watcher.Changed += WatcherOnChanged;
        watcher.Filter = "*.*";
        watcher.EnableRaisingEvents = false;
        
        while (!ct.IsCancellationRequested)
        {
            // check for new files
            await Task.Delay(1000, ct);
        }
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        // TODO: continue here
        // get face location
        using var unknownImage = MyFaceDetector.LoadImageFile(e.FullPath);
        var faceLocations = MyFaceDetector.FaceLocations(unknownImage);
     
        // TODO: delete the image when done, beware of the using above
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }
}