using System;

internal class RoutedMethod : RoutedMethodBase
{
	public RoutedMethod(Action<long> action)
	{
		this.m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action(rpc);
	}

	private Action<long> m_action;
}
