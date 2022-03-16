namespace api;

public class HostedServiceFaceDetect : IHostedService
{
    private CancellationTokenSource _cts;
    private FileSystemWatcher _watcher;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        // _task = Task.Factory.StartNew(() => Start(_cts.Token), _cts.Token);
        
        _watcher = new FileSystemWatcher("./processFolder");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
        _watcher.Created += WatcherOnCreated;
        _watcher.Changed += WatcherOnChanged;
        _watcher.Filter = "*.*";
        _watcher.EnableRaisingEvents = false;
        
        // TODO: start interval timer here to clear out expired faces from db
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
        _cts.Cancel();
        return Task.CompletedTask;
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