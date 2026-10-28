using InventoryAutoBattle.Core;

namespace InventoryAutoBattle.Core
{
    /// <summary>
    /// 토큰을 드롭받을 수 있는 대상(장비 아이템, 더미 타깃 등)이 구현하는 인터페이스
    /// </summary>
    public interface ITokenReceiver
    {
        /// <summary>
        /// 해당 토큰을 수령할 수 있는지 여부
        /// </summary>
        bool CanReceiveToken(TokenData token);

        /// <summary>
        /// 토큰 수령 처리
        /// </summary>
        bool OnReceiveToken(TokenData token);
    }
}
