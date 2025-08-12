using System;
using CriWare;
using CriWare.Assets;

public class SimpleSoundManager : IDisposable
{
    public static SimpleSoundManager Instance { get; } = new();
    private bool _disposedValue;

    public struct SimplePlayback
    {
        private readonly CriAtomExPlayer _player;
        private CriAtomExPlayback _playback;

        internal SimplePlayback(CriAtomExPlayer player, CriAtomExPlayback pb)
        {
            this._player = player;
            this._playback = pb;
        }
        
        public void SetAisacControl(string aisacControlName, float value)
        {
            this._player.SetAisacControl(aisacControlName, value);
            this._player.Update(_playback);
        }
        
        public void Pause()
        {
           _playback.Pause();
        }

        public void Resume()
        {
            _playback.Resume(CriAtomEx.ResumeMode.PausedPlayback);
        }

        public bool IsPaused()
        {
            return _playback.IsPaused();
        }
        
        public void SetVolumeAndPitch(float vol, float pitch)
        {
            this._player.SetVolume(vol);
            this._player.SetPitch(pitch);
            this._player.Update(_playback);
        }

        public void Stop()
        {
            this._playback.Stop();
        }

        public bool IsPlaying()
        {
            return this._playback.GetStatus() == CriAtomExPlayback.Status.Playing;
        }
    }
    
    public SimplePlayback StartPlayback(CriAtomExAcb acb, string cueName, float vol = 1.0f, float pitch = 1.0f)
    {
        var player = new CriAtomExPlayer();
        player.SetCue(acb, cueName);
        player.SetVolume(vol);
        player.SetPitch(pitch);
        var pb = new SimplePlayback(player, player.Start());
        return pb;
    }
    
    public SimplePlayback StartPlayback(CriAtomCueReference r, float vol = 1.0f, float pitch = 1.0f)
    {
        var player = new CriAtomExPlayer();
        player.SetCue(r.AcbAsset.Handle, r.CueId);
        player.SetVolume(vol);
        player.SetPitch(pitch);
        var pb = new SimplePlayback(player, player.Start());
        return pb;
    }

    private SimpleSoundManager()
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;
        }
    }

    ~SimpleSoundManager()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
