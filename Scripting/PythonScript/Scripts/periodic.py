oiltemp_val = OilTemp.RawValue
oilpressure_val = OilPressure.RawValue

with postgres_connection.cursor() as cursor:
	insert_variable_value(cursor, oil_temp_id, oiltemp_val)
	insert_variable_value(cursor, oil_pressure_id, oilpressure_val)
 
postgres_connection.commit()

print(f"periodic => {OilTemp.Name}/{oil_temp_id}: {oiltemp_val}")
print(f"periodic => {OilTemp.Name}/{oil_temp_id}: {oilpressure_val}")