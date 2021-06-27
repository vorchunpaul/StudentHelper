
namespace StudentHelper
{
    partial class AnswerLink
    {
        /// <summary> 
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором компонентов

        /// <summary> 
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.score_labal = new System.Windows.Forms.Label();
            this.type_label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // score_labal
            // 
            this.score_labal.AutoSize = true;
            this.score_labal.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.score_labal.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.score_labal.Location = new System.Drawing.Point(19, 11);
            this.score_labal.Name = "score_labal";
            this.score_labal.Size = new System.Drawing.Size(81, 19);
            this.score_labal.TabIndex = 0;
            this.score_labal.Text = "1,080392";
            // 
            // type_label
            // 
            this.type_label.AutoSize = true;
            this.type_label.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.type_label.ForeColor = System.Drawing.SystemColors.ControlDark;
            this.type_label.Location = new System.Drawing.Point(517, 11);
            this.type_label.Name = "type_label";
            this.type_label.Size = new System.Drawing.Size(27, 19);
            this.type_label.TabIndex = 0;
            this.type_label.Text = "DT";
            // 
            // AnswerLink
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.Controls.Add(this.type_label);
            this.Controls.Add(this.score_labal);
            this.Name = "AnswerLink";
            this.Size = new System.Drawing.Size(555, 68);
            this.DoubleClick += new System.EventHandler(this.AnswerLink_DoubleClick);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label score_labal;
        private System.Windows.Forms.Label type_label;
    }
}
