public interface IDamageable
{
    public void TakeDamageLocal(int damage, bool isFromServer = false); // When receiving damage from local object or other player
    public void TakeDamageServer(int damage); // When sending damage to another player
}
