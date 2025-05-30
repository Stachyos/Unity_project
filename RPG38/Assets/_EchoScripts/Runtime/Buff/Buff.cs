using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Runtime;
using JKFrame;
using UnityEngine;

// Buff管理器
public class BuffSystem
{
    private readonly Dictionary<int, Buff> _activeBuffs = new Dictionary<int, Buff>();
    private readonly GameObject _target;

    public BuffSystem(GameObject target)
    {
        _target = target;
    }

    // 外部驱动更新
    public void Update(float deltaTime)
    {
        var toRemove = new List<int>();
        
        foreach (var buff in _activeBuffs.Values)
        {
            buff.Update(deltaTime);
            
            if (!buff.IsPersistent && buff.Duration <= 0)
            {
                toRemove.Add(buff.Id);
            }
        }

        foreach (var id in toRemove)
        {
            RemoveBuff(id);
        }
    }

    public bool HasBuff(int buffId)
    {
        return _activeBuffs.ContainsKey(buffId);
    }
    

    public void AddBuff(int id)
    {
        if(id<1000)
            return;
        
        var dataSo = ResSystem.LoadAsset<BuffDataSo>($"Assets/_EchoAddressable/DataSo/BuffDataSo_{id}.asset");
        if (dataSo == null)
        {
            JKLog.Error("BuffDataSo is null");
            return;
        }

        Buff buff = null;
        switch (dataSo.Id)
        {
            case 1001:
                buff = new Buff_1001();
                break;
            case 1002:
                buff = new Buff_1002();
                break;
            case 1003:
                buff = new Buff_1003();
                break;
            case 1004:
                buff = new Buff_1004();
                break;
            case 1005:
                buff = new Buff_1005();
                break;
            case 1006:
                buff = new Buff_1006();
                break;
            case 1007:
                buff = new Buff_1007();
                break;
            case 1008:
                buff = new Buff_1008();
                break;
            case 1009:
                buff = new Buff_1009();
                break;
            default:
                throw new ArgumentNullException();
        }
        
        buff.Id = dataSo.Id;
        buff.Duration = dataSo.Duration;
        buff.Name = dataSo.Name;
        buff.IsPersistent = dataSo.IsPersistent;
        buff.TickInterval = dataSo.TickInterval;
        this.AddBuff(buff);
    }

    public int AddBuff(Buff buff)
    {
        RemoveBuff(buff.Id);
        buff.Apply(_target);
        _activeBuffs.Add(buff.Id, buff);
        return buff.Id;
    }

    public void RemoveBuff(int buffId)
    {
        if (_activeBuffs.TryGetValue(buffId, out var buff))
        {
            buff.Remove();
            _activeBuffs.Remove(buffId);
        }
    }

    public void ClearAllBuffs()
    {
        foreach (var buff in _activeBuffs.Values)
        {
            buff.Remove();
        }
        _activeBuffs.Clear();
    }
}

public abstract class Buff
{
    public int Id { get; set; }
    public string Name { get; set; }
    public float Duration { get; set; }
    public float TickInterval { get; set; }
    public bool IsPersistent { get; set; }
    
    private float _tickTimer;
    protected GameObject Target;

    public virtual void Apply(GameObject target)
    {
        Target = target;
        _tickTimer = TickInterval;
    }

    public virtual void Update(float deltaTime)
    {
        if (!IsPersistent)
        {
            Duration -= deltaTime;
        }
        
        if (TickInterval > 0)
        {
            _tickTimer -= deltaTime;
            if (_tickTimer <= 0)
            {
                OnTick();
                _tickTimer = TickInterval;
            }
        }
    }

    public abstract void Remove();
    public virtual void OnTick() { } // 定时触发的逻辑
}