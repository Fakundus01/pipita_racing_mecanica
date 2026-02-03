"""add servicios table

Revision ID: 003_add_servicios_table
Revises: 002_add_cliente_vehiculo_fields
Create Date: 2026-02-03 00:00:00.000000

"""
from alembic import op #type: ignore
import sqlalchemy as sa

revision = '003_add_servicios_table'
down_revision = '002_add_cliente_vehiculo_fields'
branch_labels = None
depends_on = None


def upgrade():
  op.create_table(
    'servicios',
    sa.Column('id', sa.Integer(), primary_key=True),
    sa.Column('created_at', sa.DateTime(), nullable=False),
    sa.Column('updated_at', sa.DateTime(), nullable=False),
    sa.Column('vehiculo_id', sa.Integer(), nullable=False),
    sa.Column('descripcion', sa.Text(), nullable=False),
    sa.Column('fecha', sa.Date(), nullable=False),
    sa.Column('kilometraje', sa.Integer()),
    sa.Column('costo', sa.Numeric(12, 2), nullable=False, server_default='0'),
    sa.Column('notas', sa.Text()),
    sa.ForeignKeyConstraint(['vehiculo_id'], ['vehiculos.id']),
  )


def downgrade():
  op.drop_table('servicios')