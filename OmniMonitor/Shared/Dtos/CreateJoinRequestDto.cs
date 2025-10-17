public class CreateJoinRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public JoinType JoinType { get; set; }
    public JoinOperandDto LeftOperand { get; set; }
    public JoinOperandDto RightOperand { get; set; }
}