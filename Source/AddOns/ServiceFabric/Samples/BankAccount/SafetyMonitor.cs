using Microsoft.PSharp;

namespace BankAccount
{
    /// <summary>
    /// Asserts safety of an execution.
    /// </summary>
    class SafetyMonitor : Monitor
    {
        MachineId A, B;
        int BalanceA = -1, BalanceB = -1;
        int TotalBalance = 0;

        [Start]
        [OnEventDoAction(typeof(InitializeBrokerEvent), nameof(OnInitBroker))]
        class InitBroker : MonitorState { }

        [OnEventDoAction(typeof(AccountBalanceUpdatedEvent), nameof(OnInitAccount))]
        [IgnoreEvents(typeof(InitializeBrokerEvent))]
        class InitAccount : MonitorState { }

        [Cold]
        [OnEventDoAction(typeof(AccountBalanceUpdatedEvent), nameof(OnUpdate))]
        class BalanceGood : MonitorState { }

        [Hot]
        [OnEntry(nameof(OnImbalance))]
        [OnEventDoAction(typeof(AccountBalanceUpdatedEvent), nameof(OnUpdate))]
        class BalanceBad : MonitorState { }

        void OnInitBroker()
        {
            var ev = (this.ReceivedEvent as InitializeBrokerEvent);
            A = ev.Source;
            B = ev.Target;
            this.Goto<InitAccount>();
        }

        void OnInitAccount()
        {
            var ev = (this.ReceivedEvent as AccountBalanceUpdatedEvent);
            UpdateBalance(ev);

            if (BalanceA >= 0 && BalanceB >= 0)
            {
                TotalBalance = BalanceA + BalanceB;
                this.Goto<BalanceGood>();
            }
        }

        void OnUpdate()
        {
            var ev = (this.ReceivedEvent as AccountBalanceUpdatedEvent);
            UpdateBalance(ev);
            if (BalanceA + BalanceB != TotalBalance)
            {
                this.Goto<BalanceBad>();
            }
            else
            {
                this.Goto<BalanceGood>();
            }
        }

        void UpdateBalance(AccountBalanceUpdatedEvent ev)
        {
            if (ev.Mid == A)
            {
                BalanceA = ev.Balance;
            }
            else if (ev.Mid == B)
            {
                BalanceB = ev.Balance;
            }
            else
            {
                this.Assert(false);
            }
        }

        void OnImbalance()
        {
            //this.Assert(false);
        }
    }
}
