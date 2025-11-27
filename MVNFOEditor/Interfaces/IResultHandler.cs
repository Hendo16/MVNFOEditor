using System;

namespace MVNFOEditor.Interface;

public interface IResultHandler
{
    public event EventHandler<bool> ClosePageEvent;
    public event EventHandler<bool> NextPageEvent;
}