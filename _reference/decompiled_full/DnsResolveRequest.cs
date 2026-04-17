using System;
using System.ComponentModel;
using NetworkingUtils;

public class DnsResolveRequest
{
	public readonly string m_domainName;

	private bool m_succeeded;

	private IPv6Address? m_resolvedAddress;

	private readonly BackgroundWorker m_worker;

	private bool m_completed;

	private DnsResolveRequestCompletedHandler m_callback;

	public IPv6Address? Address
	{
		get
		{
			if (!m_completed)
			{
				throw new InvalidOperationException("Must wait for worker to complete before checking if it succeeded!");
			}
			return m_resolvedAddress;
		}
	}

	public event ResolveDomainCompletedHandler Completed;

	public DnsResolveRequest(string domainName, DnsResolveRequestCompletedHandler completedCallback)
	{
		m_domainName = domainName;
		m_worker = new BackgroundWorker();
		m_worker.DoWork += delegate
		{
			m_succeeded = DnsResolver.URLToIP(m_domainName, out m_resolvedAddress);
		};
		m_worker.RunWorkerCompleted += delegate
		{
			m_completed = true;
			m_callback?.Invoke(this);
			this.Completed?.Invoke(m_succeeded, m_resolvedAddress);
		};
		m_callback = completedCallback;
	}

	public void RunAsync()
	{
		m_worker.RunWorkerAsync(this);
	}
}
