"""add cliente vehiculo fields

Revision ID: 002_add_cliente_vehiculo_fields
Revises: 001_create_core_tables
Create Date: 2025-02-14 00:30:00.000000

"""
from alembic import op #type: ignore
import sqlalchemy as sa

revision = '002_add_cliente_vehiculo_fields'
down_revision = '001_create_core_tables'
branch_labels = None
depends_on = None


def upgrade():
  op.add_column('vehiculos', sa.Column('version', sa.String(length=80)))
  op.add_column('vehiculos', sa.Column('patente', sa.String(length=20)))
  op.add_column('vehiculos', sa.Column('cliente_id', sa.Integer()))
  op.create_unique_constraint('uq_vehiculos_patente', 'vehiculos', ['patente'])
  op.create_foreign_key(
    'fk_vehiculos_cliente_id',
    'vehiculos',
    'clientes',
    ['cliente_id'],
    ['id'],
  )


def downgrade():
  op.drop_constraint('fk_vehiculos_cliente_id', 'vehiculos', type_='foreignkey')
  op.drop_constraint('uq_vehiculos_patente', 'vehiculos', type_='unique')
  op.drop_column('vehiculos', 'cliente_id')
  op.drop_column('vehiculos', 'patente')
  op.drop_column('vehiculos', 'version')